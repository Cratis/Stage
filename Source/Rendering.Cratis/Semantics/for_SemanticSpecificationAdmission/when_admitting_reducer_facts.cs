// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticSpecificationAdmission;

public class when_admitting_reducer_facts : Specification
{
    ArtifactRenderPlan _plan = null!;

    async Task Because() => _plan = await a_reducer_fact_specification.Plan();

    [Fact] void should_complete_the_plan() => _plan.Success.ShouldBeTrue();
    [Fact] void should_not_report_an_unsupported_specification() => Assert.DoesNotContain(_plan.Diagnostics, diagnostic => diagnostic.Code == "STAGE-ESM-011");
    [Fact] void should_emit_all_three_specifications() => _plan.Artifacts.Count(artifact => artifact.RelativePath.Contains("/when_score_", StringComparison.Ordinal)).ShouldEqual(3);
}

public class when_refusing_unstable_reducer_facts
{
    [Theory]
    [InlineData("context.Occurred.Year", "Occurred")]
    [InlineData("(int)context.SequenceNumber", "SequenceNumber")]
    [InlineData("context.Tenant.Length", "Tenant")]
    public async Task should_keep_the_context_refusal(string score, string member)
    {
        var source = a_reducer_fact_specification.Source.Replace(
            a_reducer_fact_specification.Body,
            $"return new TalkScore(Guid.Parse(context.Key), {score});",
            StringComparison.Ordinal);
        var plan = await a_reducer_fact_specification.Plan(source);
        var refusals = plan.Diagnostics.Where(diagnostic => diagnostic.Code == "STAGE-ESM-011").ToArray();
        Assert.Equal(3, refusals.Length);
        Assert.All(refusals, refusal => Assert.Contains(member, refusal.Message, StringComparison.Ordinal));
        Assert.DoesNotContain(plan.Artifacts, artifact => artifact.RelativePath.Contains("/when_score_", StringComparison.Ordinal));
    }

    [Fact]
    public async Task should_not_claim_an_unreached_instance()
    {
        var source = a_reducer_fact_specification.Source.Replace(
            "then readmodel TalkScore\n          talkId = \"3fa85f64-5717-4562-b3fc-2c963f66afa6\"",
            "then readmodel TalkScore\n          talkId = \"5fa85f64-5717-4562-b3fc-2c963f66afa8\"",
            StringComparison.Ordinal);
        var plan = await a_reducer_fact_specification.Plan(source);
        var refusals = plan.Diagnostics.Where(diagnostic => diagnostic.Code == "STAGE-ESM-011").ToArray();
        Assert.Equal(3, refusals.Length);
        Assert.All(refusals, refusal => Assert.Contains("no given event reaches", refusal.Message, StringComparison.Ordinal));
    }
}
#endif
