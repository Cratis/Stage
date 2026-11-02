// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using NSubstitute;
using Xunit;

namespace Cratis.Stage.Contracts.Execution.for_ModelRootBodyExecution;

public class when_compilation_times_out_once : given.a_model_root
{
    [Fact]
    async Task should_compile_again_after_the_warmup_timed_out_instead_of_pinning_the_failure()
    {
        var calls = 0;
        compiler.CompileAsync(root, Arg.Any<CancellationToken>()).Returns(_ => ++calls == 1 ? throw new TimeoutException("slow") : Task.FromResult(Manifest));
        Create();
        await using var provider = await Provider();
        (await provider.EvaluateAsync(Body, Context, CancellationToken.None)).Satisfied.ShouldEqual(true);
        calls.ShouldEqual(2);
    }
}
