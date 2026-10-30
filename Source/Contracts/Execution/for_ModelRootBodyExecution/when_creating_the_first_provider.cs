// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using NSubstitute;
using Xunit;

namespace Cratis.Stage.Contracts.Execution.for_ModelRootBodyExecution;

public class when_creating_the_first_provider : given.a_model_root
{
    [Fact]
    async Task should_compile_the_root_and_evaluate_the_manifest()
    {
        Create();
        await using var provider = await Provider();
        (await provider.EvaluateAsync(Body, Context, CancellationToken.None)).Satisfied.ShouldEqual(true);
        await compiler.Received(1).CompileAsync(root, Arg.Any<CancellationToken>());
        await ((IAsyncDisposable)compiler).Received(1).DisposeAsync();
    }
}
