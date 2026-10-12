// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticVersionFeatures.when_inspecting_reactions;

public class with_produces_and_invokes_in_separate_triggers : given.an_event_reaction
{
    void Establish() => _slice = _slice with
    {
        Reactions = [_reaction with { Triggers = [_trigger, _trigger with
        {
            Produces = [],
            Invokes = _slice.Reactions.Single(reaction => reaction.Name == "PaymentImporter").Triggers.Single().Invokes
        }] }]
    };

    void Because() => _features = [.. SemanticVersionFeatures.InSlice(_slice)];

    [Fact] void should_report_a_mixed_handler_result() => _features[0].Code.ShouldEqual("STAGE-ESM-037");
    [Fact] void should_keep_the_execution_refusal() => _features.Single(feature => feature.Kind == "reaction" && feature.Code == "STAGE-ESM-024").Message.ShouldEqual("Reaction 'ReminderScheduler' is an ESM v6 automation construct; the Stage Host and specification executor do not run reactions yet.");
}
