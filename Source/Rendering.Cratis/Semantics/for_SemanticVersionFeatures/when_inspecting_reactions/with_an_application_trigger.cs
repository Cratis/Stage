// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticVersionFeatures.when_inspecting_reactions;

public class with_an_application_trigger : given.an_event_reaction
{
    void Establish() => _trigger = _slice.Reactions.Single(reaction => reaction.Name == "PaymentImporter").Triggers.Single();
    void Because() => _features = Inspect();

    [Fact] void should_name_the_trigger_kind_before_generic_admission() => _features[0].Message.ShouldEqual("Reaction 'ReminderScheduler' uses trigger kind 'ApplicationTrigger', which Stage does not support yet.");
    [Fact] void should_report_the_automation_code() => _features[0].Code.ShouldEqual("STAGE-ESM-024");
}
