// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Specifications.Semantic;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticVersionFeatures.when_inspecting_reactions;

public class with_an_interval_trigger : given.an_event_reaction
{
    void Establish() => _trigger = _trigger with { Kind = SemanticReactionTriggerKind.Interval, Every = 60 };
    void Because() => _features = Inspect();

    [Fact] void should_name_the_trigger_kind_before_generic_admission() => _features[0].Message.ShouldEqual("Reaction 'ReminderScheduler' uses trigger kind 'Interval', which Stage does not support yet.");
    [Fact] void should_report_the_automation_code() => _features[0].Code.ShouldEqual("STAGE-ESM-024");
    [Fact] void should_report_the_reaction_capability() => _features[0].Capability.ShouldEqual(StageExecutionCapability.Reaction);
}
