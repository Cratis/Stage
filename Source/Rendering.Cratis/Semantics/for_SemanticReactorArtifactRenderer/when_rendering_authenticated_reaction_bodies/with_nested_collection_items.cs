// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticReactorArtifactRenderer.when_rendering_authenticated_reaction_bodies;

public class with_nested_collection_items : Specification
{
    ArtifactRenderPlan[] _plans = [];

    void Because()
    {
        string[] bodies =
        [
            "return [new object[] {}];",
            "return [new object[0]];",
            "object[] effects = { new object[] {} }; return effects;",
            "return new object[] { new object[] { new TicketOpened(Title) } };",
            "return [new[] { new[] { new TicketOpened(Title) } }];",
            "return [[new TicketOpened(Title)]];",
            "return [new List<object>()];",
            "return [new List<object> { new TicketOpened(Title) }];",
            "yield return new object[] {};",
            "var effects = new object[] {}; return [effects];",
            "TicketOpened[] effects = new[] { new TicketOpened(Title) }; yield return effects;",
            "TicketOpened[][] effects = new[] { new[] { new TicketOpened(Title) } }; return effects;",
            "IEnumerable<TicketOpened[]> effects = new[] { new[] { new TicketOpened(Title) } }; return [.. effects];",
            "return [.. new object[] { new object[] {} }];",
            "return [Title.Length > 0 ? new object[] {} : new object[] {}];"
        ];
        _plans = [.. bodies.SelectMany(body => new[] { false, true }.Select(file => opaque_policy_model.Plan(reaction_return_model.Load(body, file))))];
    }

    [Fact]
    void should_refuse_inline_and_file_bodies_without_publishing() => Assert.All(_plans, plan =>
    {
        Assert.False(plan.CanPublishArtifacts);
        Assert.Empty(plan.Artifacts);
    });

    [Fact]
    void should_explain_that_collection_items_are_not_flattened() => Assert.All(_plans, plan =>
        Assert.Contains(plan.Diagnostics, diagnostic => diagnostic.Code == "STAGE-ESM-037" &&
            diagnostic.Severity == ArtifactRenderDiagnosticSeverity.Error &&
            diagnostic.Message.Contains("returns a collection as a result item. Reactor side-effect handling does not flatten nested collections", StringComparison.Ordinal)));
}
#endif
