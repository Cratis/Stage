// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Reflection;
using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_exercising_translation_specifications : given.a_translation_specification_application
{
    [Fact]
    async Task should_read_explicitly_keyed_private_history_for_when_added()
    {
        _model = Transform(
            _model,
            slice => slice.Name == "TrackShipments" ? slice with
            {
                Captures = [.. slice.Captures.Select(capture => capture with { Appends = [.. capture.Appends.Select(append => append with { When = new(SemanticCaptureConditionKind.Added, []) })] })],
                Specifications = [.. slice.Specifications.Select(specification => specification with
            {
                GivenEvents = specification.ThenEvents,
                ThenEvents = [],
                ThenNoEvents = true
            })]
            } : slice,
            recovery: true);
        Render(clientOnly: true);
        await RunGenerated("when_receiving_a_shipment");
    }

    [Fact]
    async Task should_not_treat_history_at_another_key_as_the_captured_entity()
    {
        _model = Transform(
            _model,
            slice => slice.Name == "TrackShipments" ? slice with
            {
                Captures = [.. slice.Captures.Select(capture => capture with { Appends = [.. capture.Appends.Select(append => append with { When = new(SemanticCaptureConditionKind.Added, []) })] })],
                Specifications = [.. slice.Specifications.Select(specification => specification with
            {
                GivenEvents = [.. specification.ThenEvents.Select(expected => expected with
                {
                    EventSource = new(SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.Text), new SemanticTextValue("another-key"))
                })]
            })]
            } : slice);
        Render(clientOnly: true);
        await RunGenerated("when_receiving_a_shipment");
    }

    [Fact]
    async Task should_call_a_capture_without_conditions_without_a_store_dependency()
    {
        _model = Transform(_model, slice => slice.Name == "TrackShipments" ? slice with
        {
            Captures = [.. slice.Captures.Select(capture => capture with { Appends = [.. capture.Appends.Select(append => append with { When = null })] })]
        } : slice);
        Render(clientOnly: true);
        await RunGenerated("when_receiving_a_shipment");
    }

    [Fact]
    async Task should_fail_when_the_emitted_event_value_does_not_match_the_expectation()
    {
        ChangePublication(specification => specification with
        {
            ThenEvents = [.. specification.ThenEvents.Select(expected => expected with
            {
                Values = [.. expected.Values.Select(value => value with { Value = new SemanticTextValue("wrong-carrier") })]
            })]
        });
        await AssertionFails("when_publishing_a_packed_order");
    }

    [Fact]
    async Task should_fail_when_the_emitted_event_source_does_not_match_the_expectation()
    {
        _model = Transform(_model, slice => slice.Name == "TrackShipments" ? slice with
        {
            Specifications = [.. slice.Specifications.Select(specification => specification with
            {
                ThenEvents = [.. specification.ThenEvents.Select(expected => expected with
                {
                    EventSource = expected.EventSource! with { Value = new SemanticTextValue("11111111-1111-1111-1111-111111111111") }
                })]
            })]
        } : slice);
        await AssertionFails("when_receiving_a_shipment");
    }

    [Fact]
    async Task should_not_pass_a_no_events_assertion_when_the_handler_produces_an_event()
    {
        ChangePublication(specification => specification with { ThenEvents = [], ThenNoEvents = true }, recovery: true);
        await AssertionFails("when_publishing_a_packed_order");
    }

    void ChangePublication(Func<SemanticSpecification, SemanticSpecification> change, bool recovery = false) =>
        _model = Transform(_model, slice => slice.Name == "PublishOrderShipped" ? slice with { Specifications = [.. slice.Specifications.Select(change)] } : slice, recovery);

    async Task AssertionFails(string name)
    {
        Render(clientOnly: true);
        var exception = await Assert.ThrowsAsync<TargetInvocationException>(() => RunGenerated(name));
        exception.InnerException!.GetType().Namespace.ShouldEqual("Xunit.Sdk");
    }
}
#endif
