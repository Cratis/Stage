// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticVersionFeatures.when_inspecting_reactions;

public class with_an_event_trigger : given.an_event_reaction
{
    void Because() => _features = Inspect();

    [Fact] void should_keep_reaction_rendering_refused() => _features[0].Message.ShouldEqual("Reaction 'ReminderScheduler' is an ESM v6 automation construct; reaction rendering is not yet admitted.");
    [Fact] void should_report_the_automation_code() => _features[0].Code.ShouldEqual("STAGE-ESM-024");
    [Fact] void should_name_the_reaction() => _features[0].Artifact.ShouldEqual(_reaction.Id);
    [Fact] void should_report_one_reaction_refusal() => _features.Count(feature => feature.Kind == "reaction").ShouldEqual(1);
}
