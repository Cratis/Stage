// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticVersionFeatures.when_inspecting_reactions;

public class with_an_opaque_body : given.an_event_reaction
{
    void Establish() => _trigger = _trigger with { RequirementId = "opaque-reaction", Produces = [] };
    void Because() => _features = Inspect();

    [Fact] void should_keep_the_opaque_body_refusal_for_shared_execution_admission() => _features.Any(feature =>
        feature.Code == "STAGE-ESM-024" && feature.Message == "Reaction 'ReminderScheduler' has an opaque body for trigger kind 'Event', which Stage does not support yet.").ShouldBeTrue();

    [Fact] void should_offer_only_that_refusal_for_render_only_exclusion() => SemanticVersionFeatures.StandaloneReactionBodiesInSlice(
        _slice with { Reactions = [_reaction with { Triggers = [_trigger] }] }).Single().ShouldEqual(
            _features.Single(feature => feature.Message.Contains("opaque body", StringComparison.Ordinal)));
}
