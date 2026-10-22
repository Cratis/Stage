// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Serialization;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticTranslationSupport;

public class when_inspecting_translations : Specification
{
    SemanticSlice[] _slices = [];
    Dictionary<SemanticId, SemanticEventContract> _events = null!;
    SemanticCapture _capture = null!;
    SemanticProjection _publisher = null!;

    void Establish()
    {
        var model = SemanticModelSerializer.Deserialize(PublicEventsCorpus.V9.EsmBytes.AsSpan());
        _slices = [.. model.Application.Modules.SelectMany(module => module.Features).SelectMany(feature => feature.Slices)];
        _events = _slices.SelectMany(slice => slice.Events).ToDictionary(contract => contract.Id);
        _capture = _slices.SelectMany(slice => slice.Captures).Single();
        _publisher = _slices.SelectMany(slice => slice.Projections).Single();
    }

    [Fact] void should_admit_the_canonical_foreign_capture() => SemanticTranslationSupport.Capture(_capture, _events).ShouldBeTrue();
    [Fact] void should_admit_the_canonical_publication() => SemanticTranslationSupport.Projection(_publisher, _events).ShouldBeTrue();
    [Fact] void should_refuse_event_target_reducers() => SemanticTranslationSupport.Slice(
        _slices.Single(slice => slice.Projections.Contains(_publisher)) with
        {
            Reducers = [new("Publish", _publisher.ReadModel, []) { Target = SemanticProjectionTargetKind.Event }]
        },
        _events).ShouldBeFalse();
    [Fact] void should_admit_the_canonical_translation_slices() => _slices.Where(slice => slice.Kind == SemanticSliceKind.Translate).All(slice => SemanticTranslationSupport.Slice(slice, _events)).ShouldBeTrue();
    [Fact] void should_recognize_both_public_contracts() => _events.Values.Where(contract => contract.Visibility == SemanticEventVisibility.Public).All(contract => SemanticTranslationSupport.Event(contract, _slices, _events)).ShouldBeTrue();
    [Fact] void should_refuse_an_outside_system_capture() => SemanticTranslationSupport.Capture(_capture with { EventsSource = null }, _events).ShouldBeFalse();
    [Fact] void should_refuse_a_missing_source_key() => SemanticTranslationSupport.Capture(_capture with { Key = "missing" }, _events).ShouldBeFalse();
    [Fact] void should_refuse_a_mapping_that_reads_another_contract() => SemanticTranslationSupport.Projection(_publisher with { Scope = _publisher.Scope! with { From = [_publisher.Scope.From.Single() with { EventContract = _publisher.ReadModel }] } }, _events).ShouldBeFalse();
    [Fact] void should_refuse_a_read_model_target() => SemanticTranslationSupport.Projection(_publisher with { Target = SemanticProjectionTargetKind.ReadModel }, _events).ShouldBeFalse();
    [Fact] void should_refuse_an_unmapped_required_property() => SemanticTranslationSupport.Projection(_publisher with { Scope = _publisher.Scope! with { From = [_publisher.Scope.From.Single() with { Mappings = [] }] } }, _events).ShouldBeFalse();

    [Theory]
    [InlineData(SemanticCaptureConditionKind.Added, true)]
    [InlineData(SemanticCaptureConditionKind.AnyChanged, false)]
    [InlineData(SemanticCaptureConditionKind.AllChanged, false)]
    [InlineData(SemanticCaptureConditionKind.Transition, false)]
    [InlineData(SemanticCaptureConditionKind.Expression, false)]
    public void should_keep_condition_rendering_separate(SemanticCaptureConditionKind kind, bool renderable)
    {
        var capture = _capture with { Appends = [_capture.Appends.Single() with { When = new(kind, ["orderId"]) }] };
        SemanticTranslationSupport.Capture(capture, _events).ShouldBeTrue();
        SemanticTranslationSupport.ConditionsRenderable(capture).ShouldEqual(renderable);
    }
}
