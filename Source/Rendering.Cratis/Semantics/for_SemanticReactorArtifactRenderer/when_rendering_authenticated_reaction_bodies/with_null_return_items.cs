// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticReactorArtifactRenderer.when_rendering_authenticated_reaction_bodies;

public class with_null_return_items : Specification
{
    ArtifactRenderPlan[] _plans = [];

    void Because()
    {
        string[] bodies =
        [
            "return [null];",
            "yield return null;",
            "return [new TicketOpened(Title), null];",
            "var item = null; yield return item;",
            "TicketOpened item = null; return [item];",
            "TicketOpened? item = Title.Length > 0 ? new TicketOpened(Title) : null; yield return item;",
            "var items = new object[] { null }; return items;",
            "TicketOpened[] items = new TicketOpened[] { null }; return items;",
            "TicketOpened[] items = { null }; return items;",
            "return [.. new object[] { null }];",
            "return [Title.Length > 0 ? new TicketOpened(Title) : null];"
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
    void should_explain_that_null_items_are_not_skipped() => Assert.All(_plans, plan =>
        Assert.Contains(plan.Diagnostics, diagnostic => diagnostic.Code == "STAGE-ESM-037" &&
            diagnostic.Severity == ArtifactRenderDiagnosticSeverity.Error &&
            diagnostic.Message.Contains("returns a null result item. Reactor side-effect handling does not skip null items", StringComparison.Ordinal)));
}
#endif
