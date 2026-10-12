// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticReactorArtifactRenderer;

public class when_rendering_event_effects : for_SemanticAutomationArtifactRenderer.given.an_automation
{
    string _source = string.Empty;
    SemanticReaction _reaction = null!;

    void Establish()
    {
        _reaction = _located.Slice.Reactions.Single(reaction => reaction.Name == "Acknowledge");
        _reaction = _reaction with { Triggers = [_reaction.Triggers.Single(), _reaction.Triggers.Single()] };
    }

    void Because() => _source = SemanticReactorArtifactRenderer.Render(_located, _reaction, _context).Content;

    [Fact] void should_use_the_semantic_reactor_identity() => _source.ShouldContain($"ReactorAttribute(\"{_reaction.Id}\")");
    [Fact] void should_exclude_replay() => _source.ShouldContain("[global::Cratis.Chronicle.Reactors.OnceOnlyAttribute]");
    [Fact] void should_merge_triggers_into_one_event_handler() => _source.Split("IEnumerable<object> Started(", StringSplitOptions.None).Length.ShouldEqual(2);
    [Fact] void should_keep_both_authored_productions() => _source.Split("effects.Add(", StringSplitOptions.None).Length.ShouldEqual(3);
    [Fact] void should_target_the_triggering_source_explicitly() => _source.ShouldContain("EventForEventSourceId(context.EventSourceId, new global::Reactions.Work.Processing.FollowUp.Acknowledged(context.Occurred)) { Occurred = context.Occurred }");
    [Fact] void should_return_the_effect_collection() => _source.ShouldContain("return effects;");
}
