// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticReactorArtifactRenderer.when_rendering_authenticated_reaction_bodies;

public class with_value_return_items : Specification
{
    ArtifactRenderPlan[] _plans = [];

    void Because()
    {
        string[] bodies =
        [
            "return [\"unsupported\"];",
            "return [42];",
            "yield return true;",
            "return [typeof(object)];",
            "return [Title];",
            "yield return context;",
            "return [@event.Title];",
            "return [Title.Length];",
            "return [context.EventSourceId];",
            "return [context.UnrepresentedMember];",
            "return [new TicketOpened(Title), Title];",
            "var value = Title; yield return value;",
            "string label = Title; yield return label;",
            "return [new Label(Title)];",
            "return [new Details(Title)];"
        ];
        _plans = [.. bodies.SelectMany(body => new[] { false, true }.Select(file =>
            opaque_policy_model.Plan(reaction_body_model.LoadSource(
                "concept Label : String\ntype Details\n  value String\n" + reaction_body_model.Source(body, file), file ? body : null))))];
    }

    [Fact]
    void should_refuse_inline_and_file_bodies_without_publishing() => Assert.All(_plans, plan =>
    {
        Assert.False(plan.CanPublishArtifacts);
        Assert.Empty(plan.Artifacts);
    });

    [Fact]
    void should_explain_the_value_return_and_its_runtime_failure() => Assert.All(_plans, plan =>
        Assert.Contains(plan.Diagnostics, diagnostic => diagnostic.Code == "STAGE-ESM-037" &&
            diagnostic.Severity == ArtifactRenderDiagnosticSeverity.Error &&
            diagnostic.Message.Contains("returns an item that is a value, not a modeled event or command, and would fail at run time", StringComparison.Ordinal)));
}
#endif
