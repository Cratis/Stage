// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_rendering_event_target_reducers;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticTranslationSupport;

public class when_inspecting_event_target_reducers : Specification
{
    SemanticSlice _slice = null!;
    SemanticReducer _reducer = null!;
    Dictionary<SemanticId, SemanticEventContract> _events = null!;
    bool _admitted;

    async Task Establish()
    {
        var loaded = await when_rendering_a_pure_reducer.Load(and_a_reducer_publishes_a_public_event.Declaration + "\n" + and_a_reducer_publishes_a_public_event.Reducer);
        var slices = loaded.Model.Application.Modules.Single().Features.Single().Slices;
        _slice = slices.Single(slice => slice.Kind == SemanticSliceKind.Translate);
        _reducer = Assert.Single(_slice.Reducers);
        _events = slices.SelectMany(slice => slice.Events).ToDictionary(contract => contract.Id);
    }

    void Because() => _admitted = SemanticTranslationSupport.Slice(_slice, _events);

    [Fact] void should_admit_the_outbound_reducer() => _admitted.ShouldBeTrue();
    [Fact] void should_recognize_its_public_contract() => SemanticTranslationSupport.Event(_events[_reducer.ReadModel], [_slice], _events).ShouldBeTrue();
    [Fact] void should_refuse_reducers_targeting_another_event() => SemanticTranslationSupport.Reducer(_reducer, _slice with { Events = [] }, _events).ShouldBeFalse();
    [Fact] void should_refuse_reducers_folding_public_events() => SemanticTranslationSupport.Reducer(_reducer with { Transitions = [_reducer.Transitions.Single() with { EventContract = _reducer.ReadModel }] }, _slice, _events).ShouldBeFalse();
    [Fact] void should_refuse_duplicate_transitions() => SemanticTranslationSupport.Reducer(_reducer with { Transitions = [_reducer.Transitions.Single(), _reducer.Transitions.Single()] }, _slice, _events).ShouldBeFalse();
    [Fact] void should_refuse_from_filters() => SemanticTranslationSupport.Reducer(_reducer with { From = new(_slice.Id, null) }, _slice, _events).ShouldBeFalse();
    [Fact] void should_refuse_inbound_reducers() => SemanticTranslationSupport.Reducer(_reducer, _slice with { Direction = SemanticTranslationDirection.Inbound }, _events).ShouldBeFalse();
    [Fact] void should_refuse_a_private_target_declared_in_the_slice() => ReducerTargeting(_events[_reducer.ReadModel] with { Visibility = SemanticEventVisibility.Private }).ShouldBeFalse();
    [Fact] void should_refuse_a_foreign_origin_target_declared_in_the_slice() => ReducerTargeting(_events[_reducer.ReadModel] with { Origin = "OtherStore" }).ShouldBeFalse();
    [Fact] void should_refuse_foreign_sources() => SemanticTranslationSupport.Reducer(_reducer, _slice, _events.ToDictionary(pair => pair.Key, pair => pair.Key == _reducer.Transitions.Single().EventContract ? pair.Value with { Origin = "OtherStore" } : pair.Value)).ShouldBeFalse();

    bool ReducerTargeting(SemanticEventContract target) =>
        SemanticTranslationSupport.Reducer(
            _reducer,
            _slice with { Events = [.. _slice.Events.Where(contract => contract.Id != target.Id), target] },
            _events.ToDictionary(pair => pair.Key, pair => pair.Key == target.Id ? target : pair.Value));
}
#endif
