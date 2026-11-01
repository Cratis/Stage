// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticReactorArtifactRenderer.when_rendering_authenticated_reaction_bodies;

public class with_unproven_local_initializers : Specification
{
    (string Body, ArtifactRenderPlan Plan)[] _plans = [];

    void Because()
    {
        string[] bodies =
        [
            "TicketOpened[] items = new TicketOpened[1]; return items;",
            "TicketOpened[] items = new TicketOpened[Title.Length]; return items;",
            "TicketOpened[,] items = new TicketOpened[1, 2]; return items;",
            "IEnumerable<TicketOpened> items = new TicketOpened[1]; return items;",
            "Notify[] items = new Notify[1]; return items;",
            "TicketOpened[] items = Array.Empty<TicketOpened>(); return items;",
            "IEnumerable<TicketOpened> items = Array.Empty<TicketOpened>(); return items;",
            "TicketOpened item = MakeEvent(); yield return item;",
            "Notify item = MakeCommand(); yield return item;",
            "TicketOpened item = Title; yield return item;",
            "return new TicketOpened[1];",
            "var items = new TicketOpened[1]; return items;"
        ];
        _plans = [.. bodies.SelectMany(body => new[] { false, true }.Select(file => (body, opaque_policy_model.Plan(reaction_return_model.Load(body, file)))))];
    }

    [Fact]
    void should_require_initializer_proof_for_inline_and_file_bodies() => Assert.All(_plans, pair =>
    {
        Assert.Contains(pair.Plan.Diagnostics, diagnostic => diagnostic.Code == "STAGE-ESM-037" && diagnostic.Severity == ArtifactRenderDiagnosticSeverity.Error);
        Assert.False(pair.Plan.CanPublishArtifacts, pair.Body);
        Assert.Empty(pair.Plan.Artifacts);
    });
}
#endif
