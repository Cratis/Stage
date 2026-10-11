// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Serialization;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticVersionFeatures;

public class when_inspecting_event_references : Specification
{
    SemanticSlice _slice = null!;
    SemanticSlice _publisher = null!;
    SemanticCapture _capture = null!;
    SemanticEventContract _public = null!;
    SemanticEventContract _foreign = null!;
    Dictionary<SemanticId, SemanticEventContract> _events = null!;

    void Establish()
    {
        var model = SemanticModelSerializer.Deserialize(PublicEventsCorpus.V9.EsmBytes.AsSpan());
        var slices = model.Application.Modules.Single().Features.Single().Slices;
        _slice = slices.Single(slice => slice.Kind == SemanticSliceKind.StateChange);
        _publisher = slices.Single(slice => slice.Direction == SemanticTranslationDirection.Outbound);
        _public = _publisher.Events.Single();
        _events = slices.SelectMany(slice => slice.Events).ToDictionary(@event => @event.Id);
        _foreign = _events.Values.Single(@event => @event.Origin is not null);
        _capture = slices.SelectMany(slice => slice.Captures).Single();
    }

    [Theory]
    [InlineData("constraint-target", "STAGE-ESM-032")]
    [InlineData("constraint-release", "STAGE-ESM-033")]
    [InlineData("reaction-trigger", "STAGE-ESM-032")]
    [InlineData("reaction-production", "STAGE-ESM-032")]
    [InlineData("capture-source", "STAGE-ESM-032")]
    [InlineData("capture-append", "STAGE-ESM-032")]
    [InlineData("capture-child", "STAGE-ESM-032")]
    [InlineData("capture-nested", "STAGE-ESM-032")]
    [InlineData("projection-target", "STAGE-ESM-032")]
    [InlineData("reducer-target", "STAGE-ESM-032")]
    [InlineData("reducer-transition", "STAGE-ESM-032")]
    public void should_check_every_place_that_names_an_event_contract(string path, string code)
    {
        var append = _capture.Appends.Single() with { EventContract = _public.Id };
        var capture = _capture with { EventsSource = null, Appends = [], Children = [], Nested = [] };
        var constraint = new SemanticConstraint("Boundary", SemanticConstraintKind.UniqueEventOccurrence, SemanticConstraintScope.EventSequence, [new(path == "constraint-target" ? _public.Id : _slice.Events.Single().Id, [])], path == "constraint-release" ? [_foreign.Id] : [], false, null);
        var trigger = path == "reaction-trigger"
            ? new SemanticReactionTrigger(SemanticReactionTriggerKind.Event) { Source = _public.Id }
            : new SemanticReactionTrigger(SemanticReactionTriggerKind.Startup) { Produces = [new(_public.Id, null, null, [])] };
        var slice = path switch
        {
            "constraint-target" or "constraint-release" => _slice with { Constraints = [constraint] },
            "reaction-trigger" or "reaction-production" => _slice with { Reactions = [new(_capture.Id, "React", [trigger])] },
            "capture-source" => _slice with { Captures = [capture with { EventsSource = new([_public.Id]) }] },
            "capture-append" => _slice with { Captures = [capture with { Appends = [append] }] },
            "capture-child" => _slice with { Captures = [capture with { Children = [new("items", "id", [], [append])] }] },
            "capture-nested" => _slice with { Captures = [capture with { Nested = [new("item", [], [append])] }] },
            "projection-target" => _slice with { Projections = _publisher.Projections },
            "reducer-target" => _slice with { Reducers = [new("Publish", _public.Id, []) { Target = SemanticProjectionTargetKind.Event }] },
            _ => _slice with { Reducers = [new("Fold", _slice.Id, [new(_public.Id, "transition")])] }
        };
        SemanticVersionFeatures.EventReferences(slice, _events).Select(feature => feature.Code).ShouldContainOnly([code]);
    }
}
