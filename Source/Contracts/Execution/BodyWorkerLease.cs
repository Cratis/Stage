// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Contracts.Execution;

/// <summary>A per-run handle that never owns the application's worker process.</summary>
/// <param name="worker">The shared worker client that calls are forwarded to until release.</param>
internal sealed class BodyWorkerLease(IBodyWorkerClient worker) : IBodyWorkerClient, IAsyncDisposable
{
    int _released;

    public Task<BodyUnitManifest> CompileAsync(string modelPath, CancellationToken cancellationToken = default) =>
        Volatile.Read(ref _released) == 0 ? worker.CompileAsync(modelPath, cancellationToken) : Task.FromResult(new BodyUnitManifest(string.Empty, [new("*", string.Empty, false, BodyDiagnostics.WorkerFailed, Disposed().Diagnostic)]));

    public Task<BodyEvaluation> EvaluateAsync(BodyEvaluationRequest request, CancellationToken cancellationToken = default) =>
        Volatile.Read(ref _released) == 0 ? worker.EvaluateAsync(request, cancellationToken) : Task.FromResult(Disposed());

    public Task<BodyEvaluation> ValidateAsync(BodyEvaluationRequest request, CancellationToken cancellationToken = default) =>
        Volatile.Read(ref _released) == 0 ? worker.ValidateAsync(request, cancellationToken) : Task.FromResult(Disposed());

    public Task<BodyEvaluation> ReduceAsync(BodyEvaluationRequest request, CancellationToken cancellationToken = default) =>
        Volatile.Read(ref _released) == 0 ? worker.ReduceAsync(request, cancellationToken) : Task.FromResult(Disposed());

    public ValueTask DisposeAsync()
    {
        Interlocked.Exchange(ref _released, 1);
        return ValueTask.CompletedTask;
    }

    static BodyEvaluation Disposed() => new("faulted", Diagnostic: BodyDiagnostics.Format(BodyDiagnostics.WorkerFailed, "Body execution provider is disposed."));
}
