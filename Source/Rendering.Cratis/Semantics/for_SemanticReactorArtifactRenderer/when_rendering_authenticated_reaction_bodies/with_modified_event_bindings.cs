// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticReactorArtifactRenderer.when_rendering_authenticated_reaction_bodies;

public class with_modified_event_bindings : Specification
{
    ArtifactRenderPlan[] _plans = [];

    void Because()
    {
        string[] bodies =
        [
            "@event = null!; yield return @event;",
            "@event += null!; yield return @event;",
            "@event ??= new TicketOpened(Title); yield return @event;",
            "Mutate(ref @event); yield return @event;",
            "Mutate(out @event); yield return @event;",
            "Mutate(in @event); yield return @event;",
            "void Mutate() { @event = null!; } Mutate(); yield return @event;",
            "System.Action mutate = () => @event = null!; mutate(); yield return @event;",
            "yield return @event; @event = null!;",
            "ref var alias = ref @event; alias = null!; yield return @event;",
            "(@event, _) = (null!, 0); yield return @event;",
            "@event++; yield return @event;"
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
    void should_refuse_the_unproven_binding_with_the_return_shape_diagnostic() => Assert.All(_plans, plan =>
        Assert.Contains(plan.Diagnostics, diagnostic => diagnostic.Code == "STAGE-ESM-037" &&
            diagnostic.Severity == ArtifactRenderDiagnosticSeverity.Error));
}
#endif
