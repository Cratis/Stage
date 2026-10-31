// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics.Execution;

namespace Cratis.Stage.Contracts.Execution;

internal sealed class ModelRootBodyExecutionSlot(BodyUnitManifest manifest, IBodyWorkerClient worker) : IAsyncDisposable
{
    readonly Lock _gate = new();
    int _leases;
    bool _retired;
    Task? _disposal;

    public ValueTask DisposeAsync()
    {
        lock (_gate)
        {
            _disposal ??= worker is IAsyncDisposable disposable ? disposable.DisposeAsync().AsTask() : Task.CompletedTask;
            return new(_disposal);
        }
    }

    internal bool IsDisposed
    {
        get
        {
            lock (_gate) return _disposal is { IsCompleted: true };
        }
    }

    internal ISemanticBodyExecutionProvider Acquire()
    {
        lock (_gate)
        {
            _leases++;
            return new ModelRootBodyExecutionRun(new ScreenplayBodyExecutionProvider(manifest, new BodyWorkerLease(worker)), Release);
        }
    }

    internal ValueTask Retire()
    {
        lock (_gate)
        {
            _retired = true;
            return _leases == 0 ? DisposeAsync() : ValueTask.CompletedTask;
        }
    }

    ValueTask Release()
    {
        lock (_gate)
        {
            _leases--;
            return _retired && _leases == 0 ? DisposeAsync() : ValueTask.CompletedTask;
        }
    }
}
