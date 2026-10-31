// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Screenplay.Workspaces;
using Cratis.Stage.Contracts.Semantics;

namespace Cratis.Stage.Contracts.Execution;

/// <summary>
/// Compiles an on-disk model root in an isolated worker and shares execution until its files change.
/// </summary>
public sealed partial class ModelRootBodyExecution : ISemanticBodyExecutionProviderFactory, IAsyncDisposable
{
    readonly string _modelRoot;
    readonly Func<IBodyWorkerClient> _compile;
    readonly Func<BodyUnitManifest, IBodyWorkerClient> _execute;
    readonly Func<string, string> _fingerprint;
    readonly SemaphoreSlim _gate = new(1, 1);
    readonly List<ModelRootBodyExecutionSlot> _slots = [];
    readonly Task _warmup;
    readonly Lock _disposeLock = new();
    Task? _disposeTask;
    string? _revision;
    ModelRootBodyExecutionSlot? _current;
    string? _failure;
    int _disposed;

    internal int RetainedSlots => _slots.Count;

    /// <summary>
    /// Creates a factory and starts warming its compilation in the background.
    /// </summary>
    /// <param name="modelRoot">The on-disk Screenplay workspace root.</param>
    /// <param name="options">The isolated worker launch settings and resource limits.</param>
    public ModelRootBodyExecution(string modelRoot, BodyWorkerOptions options)
        : this(modelRoot, () => new BodyWorkerSupervisor(options), manifest => new BodyWorkerSupervisor(options, manifest), Fingerprint)
    {
    }

    internal ModelRootBodyExecution(string modelRoot, Func<IBodyWorkerClient> compile, Func<BodyUnitManifest, IBodyWorkerClient> execute, Func<string, string> fingerprint)
    {
        _modelRoot = Path.GetFullPath(modelRoot);
        _compile = compile;
        _execute = execute;
        _fingerprint = fingerprint;
        _warmup = Task.Run(async () =>
        {
            await _gate.WaitAsync().ConfigureAwait(false);
            try { await Refresh(CancellationToken.None).ConfigureAwait(false); }
            finally { _gate.Release(); }
        });
    }

    /// <inheritdoc/>
    /// <exception cref="ObjectDisposedException">The factory has been disposed.</exception>
    public async ValueTask<ISemanticBodyExecutionProvider> CreateAsync(SemanticApplication application, IReadOnlyDictionary<string, SemanticImplementationBody> bodies, CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposed) != 0, this);
        await _warmup.WaitAsync(cancellationToken).ConfigureAwait(false);
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposed) != 0, this);
            await Refresh(cancellationToken).ConfigureAwait(false);
            return _failure is { } failure ? new ModelRootBodyExecutionFailure(failure) : _current!.Acquire();
        }
        finally { _gate.Release(); }
    }

    /// <inheritdoc/>
    public ValueTask DisposeAsync()
    {
        lock (_disposeLock)
        {
            Interlocked.Exchange(ref _disposed, 1);
            return new(_disposeTask ??= DisposeWorkers());
        }
    }

    static string Diagnostic(string message) => message.StartsWith("STAGE-", StringComparison.Ordinal)
        ? message : BodyDiagnostics.Format(BodyDiagnostics.WorkerFailed, message);

    async Task DisposeWorkers()
    {
        await _warmup.ConfigureAwait(false);
        await _gate.WaitAsync().ConfigureAwait(false);
        try
        {
            if (_current is not null) await _current.DisposeAsync().ConfigureAwait(false);
            foreach (var slot in _slots) await slot.DisposeAsync().ConfigureAwait(false);
            _slots.Clear();
        }
        finally
        {
            _gate.Release();
            _gate.Dispose();
        }
    }

    async Task Refresh(CancellationToken cancellationToken)
    {
        string? revision = null;
        try
        {
            revision = _fingerprint(_modelRoot);
            if (revision == _revision) return;
            var manifest = await Compile(cancellationToken).ConfigureAwait(false);
            await Replace(manifest).ConfigureAwait(false);
            _revision = revision;
        }
        catch (Exception exception) when (exception is IOException or TimeoutException or InvalidSemanticModel)
        {
            if (_current is not null) await _current.Retire().ConfigureAwait(false);
            _current = null;
            _slots.RemoveAll(slot => slot.IsDisposed);
            _failure = Diagnostic(exception.Message);
            _revision = revision;
        }
    }

    async Task Replace(BodyUnitManifest manifest)
    {
        var failure = manifest.Entries.FirstOrDefault(entry => entry.RequirementId == "*");
        var next = failure is null ? new ModelRootBodyExecutionSlot(manifest, _execute(manifest)) : null;
        if (_current is not null) await _current.Retire().ConfigureAwait(false);
        _current = next;
        _slots.RemoveAll(slot => slot.IsDisposed);
        if (next is not null) _slots.Add(next);
        _failure = failure is null ? null : Diagnostic(failure.DiagnosticMessage ?? "Body compilation failed.");
    }

    async Task<BodyUnitManifest> Compile(CancellationToken cancellationToken)
    {
        var compiler = _compile();
        try
        {
            var catalog = Path.Combine(_modelRoot, ".screenplay", PersistedIdentityStateSerializer.FileName);
            return compiler is BodyWorkerSupervisor supervisor
                ? await supervisor.CompileAsync(_modelRoot, File.Exists(catalog) ? catalog : null, null, cancellationToken).ConfigureAwait(false)
                : await compiler.CompileAsync(_modelRoot, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            if (compiler is IAsyncDisposable disposable) await disposable.DisposeAsync().ConfigureAwait(false);
        }
    }
}
