// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Semantics;
using NSubstitute;
using Xunit;

namespace Cratis.Stage.Contracts.Execution.for_ModelRootBodyExecution;

public class when_compilation_fails : given.a_model_root
{
    [Theory]
    [InlineData("manifest", "STAGE-ESM-021: refused")]
    [InlineData("io", "STAGE-IMPL-009: unavailable")]
    [InlineData("timeout", "STAGE-IMPL-009: unavailable")]
    [InlineData("invalid", "STAGE-ESM-021: refused")]
    async Task should_report_the_compile_diagnostic_without_throwing(string failure, string diagnostic)
    {
        compiler.CompileAsync(root, Arg.Any<CancellationToken>()).Returns(_ => failure switch
        {
            "manifest" => Task.FromResult(new BodyUnitManifest("", [new("*", "", false, "STAGE-ESM-021", "STAGE-ESM-021: refused")])),
            "io" => throw new IOException("unavailable"),
            "timeout" => throw new TimeoutException("unavailable"),
            _ => throw new InvalidSemanticModel(["STAGE-ESM-021: refused"])
        });
        Create();
        await using var provider = await Provider();
        var result = await provider.EvaluateAsync(Body, Context, CancellationToken.None);
        result.Satisfied.ShouldBeNull();
        result.Diagnostic.ShouldEqual(diagnostic);
        (await provider.ValidateAsync(Body, Context, CancellationToken.None)).Diagnostic.ShouldEqual(diagnostic);
        var reducer = new SemanticReducerContext { Event = SemanticValue.Null, Key = SemanticValue.Text("key"), Occurred = DateTimeOffset.UnixEpoch, SequenceNumber = 0 };
        (await provider.ReduceAsync(Body, reducer, CancellationToken.None)).Diagnostic.ShouldEqual(diagnostic);
        await worker.DidNotReceive().EvaluateAsync(Arg.Any<BodyEvaluationRequest>(), Arg.Any<CancellationToken>());
    }
}
