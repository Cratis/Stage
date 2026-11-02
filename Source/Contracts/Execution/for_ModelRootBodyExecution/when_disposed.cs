// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using NSubstitute;
using Xunit;

namespace Cratis.Stage.Contracts.Execution.for_ModelRootBodyExecution;

public class when_disposed : given.a_model_root
{
    [Fact]
    async Task should_close_current_and_retiring_workers_and_reject_further_runs()
    {
        var next = Substitute.For<IBodyWorkerClient, IAsyncDisposable>();
        var count = 0;
        execution = new(root, () => compiler, _ => ++count == 1 ? worker : next, _ => fingerprint);
        await using var first = await Provider();
        fingerprint = "changed";
        await using var second = await Provider();
        await execution.DisposeAsync();
        await ((IAsyncDisposable)worker).Received(1).DisposeAsync();
        await ((IAsyncDisposable)next).Received(1).DisposeAsync();
        await Assert.ThrowsAsync<ObjectDisposedException>(() => Provider().AsTask());
    }
}
