// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using NSubstitute;
using Xunit;

namespace Cratis.Stage.Contracts.Execution.for_ModelRootBodyExecution;

public class when_the_model_root_changed : given.a_model_root
{
    [Fact]
    async Task should_retire_the_previous_worker_only_after_its_last_lease()
    {
        var next = Substitute.For<IBodyWorkerClient, IAsyncDisposable>();
        var count = 0;
        execution = new(root, () => compiler, _ => Interlocked.Increment(ref count) == 1 ? worker : next, _ => fingerprint);
        var first = await Provider();
        var second = await Provider();
        fingerprint = "changed";
        await using var third = await Provider();
        await ((IAsyncDisposable)worker).DidNotReceive().DisposeAsync();
        await first.DisposeAsync();
        await ((IAsyncDisposable)worker).DidNotReceive().DisposeAsync();
        await second.DisposeAsync();
        await ((IAsyncDisposable)worker).Received(1).DisposeAsync();
        await compiler.Received(2).CompileAsync(root, Arg.Any<CancellationToken>());
        count.ShouldEqual(2);
    }
}
