// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Stage.Contracts.Execution;

/// <summary>Supervises a short-lived compiler and a serialized execute pipe. Never loads body assemblies.</summary>
/// <param name="options">The process bounds and launch settings.</param>
public sealed class BodyWorkerSupervisor(BodyWorkerOptions options) : IBodyWorkerClient, IAsyncDisposable
{
    readonly SemaphoreSlim _gate = new(1, 1);
    readonly CancellationTokenSource _lifetime = new();
    readonly Lock _disposeLock = new();
    Task? _disposeTask;
    BodyWorkerProcess? _worker;
    BodyUnitManifest? _manifest;

    /// <summary>Creates an execute supervisor over an already compiled immutable manifest.</summary>
    /// <param name="options">The process bounds and launch settings.</param>
    /// <param name="manifest">The compiled manifest reused by this run.</param>
    public BodyWorkerSupervisor(BodyWorkerOptions options, BodyUnitManifest manifest) : this(options) => _manifest = manifest;

    /// <summary>Gets the persistent execute child's identity, or null after a kill.</summary>
    public int? ProcessId => _worker?.ProcessId;

    /// <inheritdoc/>
    public Task<BodyUnitManifest> CompileAsync(string modelPath, CancellationToken cancellationToken = default) => CompileAsync(modelPath, null, null, cancellationToken);

    /// <summary>Compiles using the same catalog and application identity as the caller's loaded model.</summary>
    /// <param name="modelPath">The source model path.</param>
    /// <param name="catalogPath">The optional catalog path.</param>
    /// <param name="applicationName">The optional application identity name.</param>
    /// <param name="cancellationToken">The caller cancellation.</param>
    /// <returns>The immutable compilation manifest.</returns>
    public async Task<BodyUnitManifest> CompileAsync(string modelPath, string? catalogPath, string? applicationName, CancellationToken cancellationToken = default)
    {
        var request = new BodyWorkerRequest(Guid.NewGuid().ToString("N"), "compile")
        {
            ModelPath = Path.GetFullPath(modelPath),
            CatalogPath = catalogPath is null ? null : Path.GetFullPath(catalogPath),
            ApplicationName = applicationName,
            CacheDirectory = Path.GetFullPath(options.CacheDirectory)
        };
        var response = await SendAsync(request, cancellationToken).ConfigureAwait(false);
        _manifest = response.Manifest ?? new(string.Empty, [new("*", string.Empty, false, BodyDiagnostics.WorkerFailed, response.Evaluation?.Diagnostic)]);
        return _manifest;
    }

    /// <inheritdoc/>
    public async Task<BodyEvaluation> EvaluateAsync(BodyEvaluationRequest request, CancellationToken cancellationToken = default)
    {
        var unit = _manifest?.Entries.FirstOrDefault(entry => entry.RequirementId == request.RequirementId);
        if (unit is null) return Failure(string.Empty, BodyDiagnostics.WorkerFailed, "No compiled unit exists for this requirement.").Evaluation!;
        if (!unit.Admitted) return new("refused", Diagnostic: unit.DiagnosticMessage);
        var response = await SendAsync(new(Guid.NewGuid().ToString("N"), "evaluate") { Unit = unit, Evaluation = request, CacheDirectory = Path.GetFullPath(options.CacheDirectory) }, cancellationToken).ConfigureAwait(false);
        return response.Evaluation!;
    }

    /// <summary>Issues one correlated request. Only a subsequent call can restart a failed execute worker.</summary>
    /// <param name="request">The correlated worker request.</param>
    /// <param name="cancellationToken">The caller cancellation.</param>
    /// <returns>The worker response or an explicit failure.</returns>
    public async Task<BodyWorkerResponse> SendAsync(BodyWorkerRequest request, CancellationToken cancellationToken = default)
    {
        using var linked = Link(cancellationToken);
        if (linked is null) return Failure(request.Id, BodyDiagnostics.WorkerFailed, "Body worker is disposed.");
        await _gate.WaitAsync(linked.Token).ConfigureAwait(false);
        BodyWorkerProcess? active = null;
        try
        {
            linked.Token.ThrowIfCancellationRequested();
            active = Worker(request.Kind);
            return await ExchangeAsync(active, request, linked.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            await KillAsync(active).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            return linked.IsCancellationRequested
                ? Failure(request.Id, BodyDiagnostics.WorkerFailed, "Body worker is shutting down.")
                : Failure(request.Id, BodyDiagnostics.ExceededTime, "Body worker exceeded its wall-time limit.");
        }
        catch (Exception exception) when (exception is IOException or JsonException or InvalidOperationException or UnauthorizedAccessException or System.ComponentModel.Win32Exception or ArgumentException)
        {
            await KillAsync(active).ConfigureAwait(false);
            return WorkerFailure(request.Id, active);
        }
        finally
        {
            if (request.Kind == "compile" && active is not null) await active.DisposeAsync().ConfigureAwait(false);
            _gate.Release();
        }
    }

    /// <inheritdoc/>
    public ValueTask DisposeAsync()
    {
        lock (_disposeLock) return new(_disposeTask ??= DisposeCoreAsync());
    }

    static BodyWorkerResponse Failure(string id, string code, string message) => new(id)
    {
        Evaluation = new(code switch { BodyDiagnostics.ExceededTime => "exceeded-time", BodyDiagnostics.ExceededMemory => "exceeded-memory", _ => "faulted" }, Diagnostic: BodyDiagnostics.Format(code, message))
    };

    static bool ValidResponse(BodyWorkerRequest request, BodyWorkerResponse response) => response.Id == request.Id && request.Kind switch
    {
        "compile" => response.Manifest is not null || response.Evaluation is not null,
        "evaluate" => response.Evaluation is not null && new[] { "satisfied", "unsatisfied", "refused", "faulted", "exceeded-time", "exceeded-memory" }.Contains(response.Evaluation.Outcome, StringComparer.Ordinal),
        _ => true
    };

    static BodyWorkerResponse WorkerFailure(string id, BodyWorkerProcess? process) => process?.ExceededMemory == true
        ? Failure(id, BodyDiagnostics.ExceededMemory, "Body worker exhausted its memory limit.")
        : Failure(id, BodyDiagnostics.WorkerFailed, "Body worker failed or returned an invalid response.");

    BodyWorkerProcess Worker(string kind) => kind == "compile" ? new(options, "compile") : _worker ??= new(options, "execute");

    CancellationTokenSource? Link(CancellationToken caller)
    {
        lock (_disposeLock) return _disposeTask is null ? CancellationTokenSource.CreateLinkedTokenSource(caller, _lifetime.Token) : null;
    }

    async Task<BodyWorkerResponse> ExchangeAsync(BodyWorkerProcess process, BodyWorkerRequest request, CancellationToken token)
    {
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(token);
        deadline.CancelAfter(request.Kind == "compile" ? options.CompileTimeoutMs : options.TimeoutMs);
        await process.StartAsync(deadline.Token).ConfigureAwait(false);
        var response = await process.ExchangeAsync(request, deadline.Token).ConfigureAwait(false);
        if (!ValidResponse(request, response)) throw new IOException("Worker response did not match the request.");
        return response;
    }

    async Task DisposeCoreAsync()
    {
        await _lifetime.CancelAsync().ConfigureAwait(false);
        await _gate.WaitAsync().ConfigureAwait(false);
        try
        {
            await KillAsync(_worker).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
            _gate.Dispose();
            _lifetime.Dispose();
        }
    }

    async Task KillAsync(BodyWorkerProcess? process)
    {
        if (process is null) return;
        await process.DisposeAsync().ConfigureAwait(false);
        if (ReferenceEquals(process, _worker)) _worker = null;
    }
}
