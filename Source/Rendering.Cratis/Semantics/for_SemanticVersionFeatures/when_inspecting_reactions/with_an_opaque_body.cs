// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticVersionFeatures.when_inspecting_reactions;

public class with_an_opaque_body : given.an_event_reaction
{
    void Establish() => _trigger = _trigger with { RequirementId = "opaque-reaction", Produces = [] };
    void Because() => _features = Inspect();

    [Fact] void should_name_the_opaque_body_and_trigger_kind_before_generic_admission() => _features[0].Message.ShouldEqual("Reaction 'ReminderScheduler' has an opaque body for trigger kind 'Event', which Stage does not support yet.");
    [Fact] void should_report_the_automation_code() => _features[0].Code.ShouldEqual("STAGE-ESM-024");
}
