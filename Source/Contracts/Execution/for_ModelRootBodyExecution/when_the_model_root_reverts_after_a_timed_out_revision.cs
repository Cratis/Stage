// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using NSubstitute;
using Xunit;

namespace Cratis.Stage.Contracts.Execution.for_ModelRootBodyExecution;

public class when_the_model_root_reverts_after_a_timed_out_revision : given.a_model_root
{
    [Fact]
    async Task should_recompile_the_reverted_revision_instead_of_returning_the_timeout()
    {
        var calls = 0;
        compiler.CompileAsync(root, Arg.Any<CancellationToken>()).Returns(_ => ++calls == 2 ? throw new TimeoutException("slow") : Task.FromResult(Manifest));
        Create();
        await using (var first = await Provider())
        {
            (await first.EvaluateAsync(Body, Context, CancellationToken.None)).Satisfied.ShouldEqual(true);
        }

        fingerprint = "edited";
        await using (var edited = await Provider())
        {
            (await edited.EvaluateAsync(Body, Context, CancellationToken.None)).Satisfied.ShouldBeNull();
        }

        fingerprint = "first";
        await using var reverted = await Provider();
        (await reverted.EvaluateAsync(Body, Context, CancellationToken.None)).Satisfied.ShouldEqual(true);
        calls.ShouldEqual(3);
    }
}
