// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Contracts.Semantics;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticReactorArtifactRenderer.when_rendering_authenticated_reaction_bodies;

public class with_undeterminable_return_effects : Specification
{
    LoadedSemanticModel[] _models = [];
    ArtifactRenderPlan[] _plans = [];

    void Establish()
    {
        string[] bodies =
        [
            "return BuildEffects();",
            "string label = Title; yield return label;",
            "IEnumerable<object> effects = new[] { new TicketOpened(Title) }; return effects;",
            "TicketOpened opened = new TicketOpened(Title); opened = new TicketOpened(Title); yield return opened;",
            "TicketOpened[] opened = new[] { new TicketOpened(Title) }; Change(opened); return opened;",
            "TicketOpened opened = new TicketOpened(Title); TicketOpened Alias() { return opened; } yield return opened;",
            "#if DEBUG\nreturn BuildEffects();\n#else\nreturn null;\n#endif",
            "object effects = new object[] { new TicketOpened(Title) }; return (IEnumerable<object>)effects;",
            "dynamic effects = BuildEffects(); return effects;",
            "var effects = new object[] { new TicketOpened(Title) }; Change(effects); return effects;",
            "var effects = new object[] { new TicketOpened(Title) }; return Change(effects) ? effects : null;",
            "var effects = new object[] { new TicketOpened(Title) }; object[] Alias() { return effects; } Change(Alias()); return effects;",
            "var effects = new object[] { new TicketOpened(Title) }; effects[0] = new Notify(\"id\", Title); return effects;",
            "return new object[] { new TicketOpened(Title), Unknown() };",
            "return typeof(object).Assembly.GetTypes();",
            "return Array.Empty<string>();",
            "return [@event.Title.Length];",
            "return [context.EventSourceId.Value];",
            "return [Title.ToString()];"
        ];
        _models = [.. bodies.SelectMany(body => new[] { reaction_return_model.Load(body), reaction_return_model.Load(body, file: true) })];
    }

    void Because() => _plans = [.. _models.Select(opaque_policy_model.Plan)];

    [Fact]
    void should_fail_closed_without_emitting_artifacts() => Assert.All(_plans, plan =>
    {
        Assert.False(plan.CanPublishArtifacts);
        Assert.Empty(plan.Artifacts);
    });
    [Fact]
    void should_offer_the_same_diagnostic_and_remedy_as_mixed_returns() => Assert.All(_plans, plan =>
        Assert.Contains(plan.Diagnostics, diagnostic => diagnostic.Code == "STAGE-ESM-037" &&
            diagnostic.Message.Contains("use two reactions, or produce the event and let a second reaction invoke the command", StringComparison.Ordinal)));
}
#endif
