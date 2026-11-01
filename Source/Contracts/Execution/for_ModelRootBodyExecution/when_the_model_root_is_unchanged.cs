// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using NSubstitute;
using Xunit;

namespace Cratis.Stage.Contracts.Execution.for_ModelRootBodyExecution;

public class when_the_model_root_is_unchanged : given.a_model_root
{
    [Fact]
    async Task should_share_compilation_and_execution()
    {
        Create();
        await using var first = await Provider();
        await using var second = await Provider();
        await first.EvaluateAsync(Body, Context, CancellationToken.None);
        await second.EvaluateAsync(Body, Context, CancellationToken.None);
        await compiler.Received(1).CompileAsync(root, Arg.Any<CancellationToken>());
        await worker.Received(2).EvaluateAsync(Arg.Any<BodyEvaluationRequest>(), Arg.Any<CancellationToken>());
    }
}
