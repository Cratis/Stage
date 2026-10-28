// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Stage.Contracts.Execution;

/// <summary>Supervises one serialized worker pipe. No body assembly is loaded into this process.</summary>
public sealed class BodyWorkerSupervisor(BodyWorkerOptions options) : IAsyncDisposable
{
    readonly SemaphoreSlim _gate = new(1, 1);
    readonly CancellationTokenSource _lifetime = new();
    readonly Lock _disposeLock = new();
    Task? _disposeTask;
    BodyWorkerProcess? _worker;
    bool _disposed;

    /// <summary>Gets the running child identity for lifecycle observations, or null after a kill.</summary>
    public int? ProcessId => _worker?.ProcessId;

    /// <summary>Issues one request. Failed workers are killed; only a subsequent request may start another.</summary>
    public async Task<BodyWorkerResponse> SendAsync(BodyWorkerRequest request, CancellationToken cancellationToken = default)
    {
        if (_disposed) return Failure(request.Id, BodyDiagnostics.WorkerFailed, "Body worker is disposed.");
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _lifetime.Token);
        var token = linked.Token;
        try
        {
            await _gate.WaitAsync(token);
        }
        catch (OperationCanceledException)
        {
            return Failure(request.Id, BodyDiagnostics.WorkerFailed, "Body evaluation was cancelled.");
        }
        try
        {
            if (_worker is null)
            {
                _worker = new(options);
                using var startup = CancellationTokenSource.CreateLinkedTokenSource(token);
                startup.CancelAfter(options.CompileTimeoutMs);
                await _worker.StartAsync(startup.Token);
            }
            using var deadline = CancellationTokenSource.CreateLinkedTokenSource(token);
            deadline.CancelAfter(request.Kind == "compile" ? options.CompileTimeoutMs : options.TimeoutMs);
            var response = await _worker.ExchangeAsync(request, deadline.Token);
            if (response.Id != request.Id) throw new IOException("Worker response correlation mismatch.");
            return response;
        }
        catch (OperationCanceledException)
        {
            await KillAsync();
            return token.IsCancellationRequested
                ? Failure(request.Id, BodyDiagnostics.WorkerFailed, "Body evaluation was cancelled.")
                : Failure(request.Id, BodyDiagnostics.ExceededTime, "Body worker exceeded its wall-time limit.");
        }
        catch (Exception exception) when (exception is IOException or JsonException or InvalidOperationException or System.ComponentModel.Win32Exception or ArgumentException)
        {
            var memory = _worker?.ExceededMemory == true;
            await KillAsync();
            return Failure(request.Id, memory ? BodyDiagnostics.ExceededMemory : BodyDiagnostics.WorkerFailed, memory ? "Body worker exhausted its memory limit." : "Body worker failed or returned an invalid response.");
        }
        finally
        {
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
        Evaluation = new("unsupported", Diagnostic: BodyDiagnostics.Format(code, message))
    };

    async Task DisposeCoreAsync()
    {
        _disposed = true;
        await _lifetime.CancelAsync();
        await _gate.WaitAsync();
        try
        {
            await KillAsync();
        }
        finally
        {
            _gate.Release();
            _gate.Dispose();
            _lifetime.Dispose();
        }
    }

    async Task KillAsync()
    {
        if (_worker is null) return;
        await _worker.DisposeAsync();
        _worker = null;
    }
}
