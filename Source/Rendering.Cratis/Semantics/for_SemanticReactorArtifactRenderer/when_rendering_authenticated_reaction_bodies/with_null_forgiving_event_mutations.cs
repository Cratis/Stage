// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticReactorArtifactRenderer.when_rendering_authenticated_reaction_bodies;

public class with_null_forgiving_event_mutations : Specification
{
    ArtifactRenderPlan[] _plans = [];

    void Because()
    {
        string[] mutations =
        [
            "@event! = null!;",
            "(@event!) = null!;",
            "@event! += null!;",
            "@event! ??= new TicketOpened(Title);",
            "@event!++;",
            "++(@event!);",
            "Mutate(out @event!);",
            "Mutate(ref @event!);",
            "Mutate(in @event!);",
            "Mutate(out (@event!));",
            "var x = 0; (@event!, x) = (null!, 0);",
            "ref var alias = ref @event; alias = null!;",
            "ref var alias = ref @event!; alias = null!;",
            "ref readonly var alias = ref @event;",
            "ref readonly var alias = ref (@event!);",
            "ref TicketOpened Alias() { return ref @event; }",
            "ref TicketOpened Alias() { return ref (@event!); }",
            "System.Action mutate = () => (@event!) = null!; mutate();"
        ];
        _plans = [.. mutations.SelectMany(mutation => new[]
        {
            $"{mutation} yield return @event;",
            $"{mutation} yield return new global::Cratis.Chronicle.EventSequences.EventForEventSourceId(context.EventSourceId, @event);"
        }).SelectMany(body => new[] { false, true }.Select(file => opaque_policy_model.Plan(reaction_return_model.Load(body, file))))];
    }

    [Fact]
    void should_refuse_inline_and_file_bodies_without_publishing() => Assert.All(_plans, plan =>
    {
        Assert.False(plan.CanPublishArtifacts);
        Assert.Empty(plan.Artifacts);
    });

    [Fact]
    void should_report_the_unproven_return_shape() => Assert.All(_plans, plan =>
        Assert.Contains(plan.Diagnostics, diagnostic => diagnostic.Code == "STAGE-ESM-037" &&
            diagnostic.Severity == ArtifactRenderDiagnosticSeverity.Error));
}
#endif
