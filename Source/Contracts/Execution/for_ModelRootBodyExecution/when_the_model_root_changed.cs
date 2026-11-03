// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using NSubstitute;
using Xunit;

namespace Cratis.Stage.Contracts.Execution.for_ModelRootBodyExecution;

public class when_the_model_root_changed : given.a_model_root
{
    int _workerCount;
    int _disposalsBeforeReleasing;
    int _disposalsAfterFirstRelease;

    async Task Because()
    {
        var next = Substitute.For<IBodyWorkerClient, IAsyncDisposable>();
        execution = new(root, () => compiler, _ => Interlocked.Increment(ref _workerCount) == 1 ? worker : next, _ => fingerprint);
        var first = await Provider();
        var second = await Provider();
        fingerprint = "changed";
        await using var third = await Provider();
        _disposalsBeforeReleasing = worker.ReceivedCalls().Count(call => call.GetMethodInfo().Name == nameof(IAsyncDisposable.DisposeAsync));
        await first.DisposeAsync();
        _disposalsAfterFirstRelease = worker.ReceivedCalls().Count(call => call.GetMethodInfo().Name == nameof(IAsyncDisposable.DisposeAsync));
        await second.DisposeAsync();
    }

    [Fact] public void should_keep_the_previous_worker_while_both_leases_are_held() => _disposalsBeforeReleasing.ShouldEqual(0);
    [Fact] public void should_keep_the_previous_worker_after_its_first_lease_is_released() => _disposalsAfterFirstRelease.ShouldEqual(0);
    [Fact] public async Task should_retire_the_previous_worker_after_its_last_lease() => await ((IAsyncDisposable)worker).Received(1).DisposeAsync();
    [Fact] public async Task should_compile_each_fingerprint_once() => await compiler.Received(2).CompileAsync(root, Arg.Any<CancellationToken>());
    [Fact] public void should_create_two_workers() => _workerCount.ShouldEqual(2);
}
