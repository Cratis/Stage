// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Contracts.Semantics;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticReactorArtifactRenderer.when_rendering_authenticated_reaction_bodies;

public class with_mixed_return_effects : Specification
{
    LoadedSemanticModel[] _models = [];
    ArtifactRenderPlan[] _plans = [];

    void Establish()
    {
        string[] bodies =
        [
            "return new object[] { new TicketOpened(Title), new Notify(\"id\", Title) };",
            "return new object[] { new Cratis.Chronicle.EventSequences.EventForEventSourceId(context.EventSourceId, new TicketOpened(Title)), new Notify(\"id\", Title) };",
            "return new[] { new TicketOpened(Title), (object)new Notify(\"id\", Title) };",
            "return [new TicketOpened(Title), new Notify(\"id\", Title)];",
            "yield return new TicketOpened(Title); yield return new Notify(\"id\", Title);",
            "TicketOpened opened = new TicketOpened(Title); Notify command = new Notify(\"id\", Title); yield return opened; yield return command;",
            "if (Title.Length > 0) return new object[] { new TicketOpened(Title) }; return new object[] { new Notify(\"id\", Title) };",
            "return Title.Length > 0 ? new object[] { new TicketOpened(Title) } : new object[] { new Notify(\"id\", Title) };"
        ];
        _models = [.. bodies.SelectMany(body => new[] { reaction_return_model.Load(body), reaction_return_model.Load(body, file: true) })];
    }

    void Because() => _plans = [.. _models.Select(opaque_policy_model.Plan)];

    [Fact] void should_refuse_inline_and_file_bodies() => Assert.All(_plans, plan => Assert.False(plan.CanPublishArtifacts));
    [Fact] void should_emit_no_artifacts() => Assert.All(_plans, plan => Assert.Empty(plan.Artifacts));
    [Fact]
    void should_name_the_non_atomic_return_and_its_remedy() => Assert.All(_plans, plan =>
        Assert.Contains(plan.Diagnostics, diagnostic => diagnostic.Code == "STAGE-ESM-037" &&
            diagnostic.Severity == ArtifactRenderDiagnosticSeverity.Error &&
            diagnostic.Message.Contains("use two reactions, or produce the event and let a second reaction invoke the command", StringComparison.Ordinal)));
}
#endif
