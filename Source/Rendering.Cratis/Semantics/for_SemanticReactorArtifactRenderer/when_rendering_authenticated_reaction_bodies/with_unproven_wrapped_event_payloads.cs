// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticReactorArtifactRenderer.when_rendering_authenticated_reaction_bodies;

public class with_unproven_wrapped_event_payloads : Specification
{
    ArtifactRenderPlan[] _plans = [];

    void Because()
    {
        string[] payloads =
        [
            "null!",
            "null",
            "42",
            "new object[] { new TicketOpened(Title) }",
            "new Notify(\"id\", Title)",
            "GetEvent()",
            "new global::Cratis.Chronicle.EventSequences.EventForEventSourceId(context.EventSourceId, new TicketOpened(Title))",
            "Title.Length > 0 ? new TicketOpened(Title) : null",
            "Title.Length switch { _ => new TicketOpened(Title) }",
            "GetEvent() ?? new TicketOpened(Title)",
            "(TicketOpened)GetEvent()",
            "@event with { Title = Title }"
        ];
        _plans = [.. payloads.SelectMany(payload => new[]
        {
            $"yield return new global::Cratis.Chronicle.EventSequences.EventForEventSourceId(context.EventSourceId, {payload});",
            $"yield return new global::Cratis.Chronicle.EventSequences.EventForEventSourceId(Event: {payload}, EventSourceId: context.EventSourceId);",
            $"yield return new global::Cratis.Chronicle.EventSequences.EventForEventSourceId(context.EventSourceId, new TicketOpened(Title)) {{ Event = {payload} }};"
        }).SelectMany(body => new[] { false, true }.Select(file => opaque_policy_model.Plan(reaction_return_model.Load(body, file))))];
    }

    [Fact]
    void should_refuse_inline_and_file_bodies_without_publishing() => Assert.All(_plans, plan =>
    {
        Assert.False(plan.CanPublishArtifacts);
        Assert.Empty(plan.Artifacts);
    });

    [Fact]
    void should_explain_that_the_payload_requires_one_proven_event() => Assert.All(_plans, plan =>
        Assert.Contains(plan.Diagnostics, diagnostic => diagnostic.Code == "STAGE-ESM-037" &&
            diagnostic.Severity == ArtifactRenderDiagnosticSeverity.Error &&
            diagnostic.Message.Contains("EventForEventSourceId payload is not proven to be a single non-null modeled event", StringComparison.Ordinal)));
}
#endif
