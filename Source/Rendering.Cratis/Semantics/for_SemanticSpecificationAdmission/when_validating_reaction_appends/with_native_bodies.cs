// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Text;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticReactorArtifactRenderer.when_rendering_authenticated_reaction_bodies;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticSpecificationAdmission.when_validating_reaction_appends;

public class with_native_bodies
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void should_emit_specs_that_await_the_rendered_reactor_and_assert_body_effects(bool file)
    {
        var plan = native_reaction_model.Plan(file: file);
        Assert.True(plan.Success, opaque_policy_model.Errors(plan));
        Assert.Empty(plan.Refusals);
        var specs = plan.Artifacts.Where(artifact => artifact.RelativePath.Contains("/when_", StringComparison.Ordinal)).Select(Text).ToArray();
        Assert.Equal(2, specs.Length);
        Assert.All(specs, code => Assert.Contains("await _reactor.Given.ForEventSource", code, StringComparison.Ordinal));
        Assert.Contains(specs, code => code.Contains("ProducedEvents.Count.ShouldEqual(1)", StringComparison.Ordinal) && code.Contains("should_preserve_destination_1", StringComparison.Ordinal));
        Assert.Contains(specs, code => code.Contains("ProducedEvents.Count.ShouldEqual(0)", StringComparison.Ordinal) && code.Contains("Produced.Count.ShouldEqual(0)", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("return context is not null ? new object[] { new TalkHighlighted(@event.Score) } : null;", "context reads")]
    [InlineData("return [new TalkHighlighted(global::System.Random.Shared.Next())];", "calls")]
    public void should_keep_unsupported_body_specs_omitted_without_removing_the_reactor(string body, string reason)
    {
        var plan = native_reaction_model.Plan(body);
        Assert.True(plan.Success, opaque_policy_model.Errors(plan));
        Assert.Equal(2, plan.Refusals.Length);
        Assert.All(plan.Refusals, refusal => Assert.Contains("STAGE-ESM-011", refusal.Codes));
        Assert.Contains(plan.Diagnostics, diagnostic => diagnostic.Message.Contains(reason, StringComparison.Ordinal));
        Assert.Contains(plan.Artifacts, artifact => artifact.RelativePath.EndsWith("HighlightTopTalk.cs", StringComparison.Ordinal));
    }

    [Fact]
    public void should_admit_deterministic_locals_and_bound_input_properties()
    {
        var body = native_reaction_model.Body.Replace("if (@event.Score", "var score = Score; if (score", StringComparison.Ordinal);
        var plan = native_reaction_model.Plan(body);
        Assert.Empty(plan.Refusals);
        Assert.True(plan.Success, opaque_policy_model.Errors(plan));
    }

    [Fact]
    public void should_omit_specs_that_would_require_settling_a_deeper_cascade()
    {
        var source = native_reaction_model.Source().Replace("      specification HighScoreHighlights", "      reaction ContinueHighlighting\n        when TalkHighlighted\n          produces TalkAccepted\n            score = score\n      specification HighScoreHighlights", StringComparison.Ordinal);
        var plan = opaque_policy_model.Plan(reaction_body_model.LoadSource(source));
        Assert.True(plan.Success, opaque_policy_model.Errors(plan));
        Assert.Equal(2, plan.Refusals.Length);
        Assert.Contains(plan.Diagnostics, diagnostic => diagnostic.Message.Contains("deeper reaction cascades", StringComparison.Ordinal));
    }

    [Fact]
    public void should_keep_undeterminable_returns_refused()
    {
        var plan = native_reaction_model.Plan("return MakeEffects();");
        Assert.False(plan.Success);
        Assert.Contains(plan.Diagnostics, diagnostic => diagnostic.Code == "STAGE-ESM-037");
    }

    static string Text(PlannedArtifact artifact) => Encoding.UTF8.GetString(artifact.Bytes.AsSpan());
}
#endif
