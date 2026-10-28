// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticOccurrenceAdmission.when_admitting_application_trigger_reactions;

public class with_a_mixed_event_trigger : given.an_application_trigger_reaction
{
    SemanticSlice _slice = null!;
    SemanticVersionFeature[] _features = [];

    void Establish()
    {
        var eventTrigger = _trigger with { Kind = SemanticReactionTriggerKind.Event, Source = _located.Slice.Events.Single().Id };
        _slice = _located.Slice with { Reactions = [_reaction with { Triggers = [_trigger, eventTrigger] }] };
    }

    void Because() => _features = [.. SemanticOccurrenceAdmission.RefusalsInSlice(_slice, _context)];

    [Fact]
    void should_refuse_a_trigger_combined_with_an_event_trigger() =>
        _features.Single().Message.ShouldContain("combines application trigger 'BatchRequested' with other triggers");
}
