// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Text;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using NSubstitute;
using Xunit;

namespace Cratis.Stage.Contracts.Execution.for_ScreenplayBodyExecutionProvider;

public class when_evaluating_a_rule : Specification
{
    [Theory]
    [InlineData("satisfied", true)]
    [InlineData("unsatisfied", false)]
    [InlineData("exceeded-time", null)]
    [InlineData("exceeded-memory", null)]
    [InlineData("faulted", null)]
    async Task should_map_the_worker_verdict_without_turning_failures_into_rejections(string outcome, bool? satisfied)
    {
        var worker = Substitute.For<IBodyWorkerClient>();
        worker.EvaluateAsync(Arg.Any<BodyEvaluationRequest>(), Arg.Any<CancellationToken>()).Returns(new BodyEvaluation(outcome));
        await using var provider = new ScreenplayBodyExecutionProvider(Manifest(), worker);
        var result = await provider.EvaluateAsync(Body(), Context(), CancellationToken.None);
        result.Satisfied.ShouldEqual(satisfied);
        (result.Diagnostic is null).ShouldEqual(satisfied.HasValue);
    }

    [Fact]
    async Task should_preserve_an_admission_refusal_without_calling_the_worker()
    {
        var worker = Substitute.For<IBodyWorkerClient>();
        var manifest = new BodyUnitManifest("revision", [new("rule", "RulePredicate", false, "STAGE-ESM-022", "STAGE-ESM-022: Refused by renderer.")]);
        await using var provider = new ScreenplayBodyExecutionProvider(manifest, worker);
        var result = await provider.EvaluateAsync(Body(), Context(), CancellationToken.None);
        result.Satisfied.ShouldBeNull();
        result.Diagnostic.ShouldEqual("STAGE-ESM-022: Refused by renderer.");
        await worker.DidNotReceive().EvaluateAsync(Arg.Any<BodyEvaluationRequest>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("missing")]
    [InlineData("tampered")]
    async Task should_refuse_unknown_or_changed_bodies_before_a_worker_call(string invalid)
    {
        var worker = Substitute.For<IBodyWorkerClient>();
        var body = Body();
        body = invalid == "tampered" ? body with { Content = "return false;" } : body with { Requirement = body.Requirement with { RequirementId = "absent" } };
        await using var provider = new ScreenplayBodyExecutionProvider(Manifest(), worker);
        var result = await provider.EvaluateAsync(body, Context(), CancellationToken.None);
        result.Satisfied.ShouldBeNull();
        result.Diagnostic!.StartsWith(BodyDiagnostics.WorkerFailed, StringComparison.Ordinal).ShouldBeTrue();
        await worker.DidNotReceive().EvaluateAsync(Arg.Any<BodyEvaluationRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    async Task should_forward_only_portable_members_and_the_callers_cancellation()
    {
        var worker = Substitute.For<IBodyWorkerClient>();
        worker.EvaluateAsync(Arg.Any<BodyEvaluationRequest>(), Arg.Any<CancellationToken>()).Returns(new BodyEvaluation("satisfied"));
        using var cancellation = new CancellationTokenSource();
        await using var provider = new ScreenplayBodyExecutionProvider(Manifest(), worker);
        await provider.EvaluateAsync(Body(), Context(), cancellation.Token);
        await worker.Received().EvaluateAsync(Arg.Is<BodyEvaluationRequest>(request => request.Members.Count == 4 && !request.Members.ContainsKey("Tenant") && !request.Members.ContainsKey("CausedBy")), cancellation.Token);
        await cancellation.CancelAsync();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await provider.EvaluateAsync(Body(), Context(), cancellation.Token));
    }

    static SemanticImplementationBody Body() => new(
        new(SemanticImplementationRole.RulePredicate, default!, null, "csharp", null, Hash(), null!) { RequirementId = "rule" }, "return true;");

    static BodyUnitManifest Manifest() => new("revision", [new("rule", "RulePredicate", true) { BodySha256 = Hash() }]);

    static string Hash() => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes("return true;"))).ToLowerInvariant();

    static SemanticRuleContext Context() => new(SemanticValue.Text("artifact"), SemanticValue.Number(1), "amount", "not-forwarded", null, DateTimeOffset.UnixEpoch);
}
