// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticVersionFeatures.when_inspecting_reactions;

public class with_produces_and_invokes_in_one_trigger : given.an_event_reaction
{
    void Establish() => _trigger = _trigger with { Invokes = _slice.Reactions.Single(reaction => reaction.Name == "PaymentImporter").Triggers.Single().Invokes };
    void Because() => _features = Inspect();

    [Fact] void should_report_the_mixed_effect_code_before_generic_admission() => _features[0].Code.ShouldEqual("STAGE-ESM-037");
    [Fact] void should_name_the_arc_limitation() => _features[0].Message.ShouldEqual("Reaction 'ReminderScheduler' both produces events and invokes commands in one 'Event' trigger; Arc cannot return events and commands from one handler (Cratis/Arc#3156).");
    [Fact] void should_keep_generic_reaction_admission() => _features.Select(feature => feature.Code).ShouldContain("STAGE-ESM-024");
}
