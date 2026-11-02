// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using NSubstitute;
using Xunit;

namespace Cratis.Stage.Contracts.Execution.for_ModelRootBodyExecution;

public class when_warming_up : given.a_model_root
{
    [Fact]
    async Task should_start_in_the_background_and_await_the_same_compilation()
    {
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var completed = new TaskCompletionSource<BodyUnitManifest>(TaskCreationOptions.RunContinuationsAsynchronously);
        compiler.CompileAsync(root, Arg.Any<CancellationToken>()).Returns(_ =>
        {
            started.SetResult();
            return completed.Task;
        });
        Create();
        await started.Task.WaitAsync(TimeSpan.FromSeconds(10));
        var provider = Provider().AsTask();
        provider.IsCompleted.ShouldBeFalse();
        completed.SetResult(Manifest);
        await using var result = await provider;
        await compiler.Received(1).CompileAsync(root, Arg.Any<CancellationToken>());
    }
}
