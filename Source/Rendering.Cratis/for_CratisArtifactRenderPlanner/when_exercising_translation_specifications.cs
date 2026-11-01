// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Reflection;
using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

[PerFactSpecificationLifecycle]
public class when_exercising_translation_specifications : given.a_translation_specification_application
{
    const string OtherKey = "9fa85f64-5717-4562-b3fc-2c963f66afa9";

    [Fact]
    async Task should_append_nothing_for_an_unchanged_foreign_record()
    {
        ChangeCapture(null, recovery: true, change: specification => specification with
        {
            GivenEvents = [new(specification.WhenAppended.EventContract, specification.WhenAppended.Values)],
            ThenEvents = [],
            ThenNoEvents = true
        });
        Render(clientOnly: true);
        await RunGenerated("when_receiving_a_shipment");
    }

    [Fact]
    async Task should_fail_an_unchanged_record_expectation_if_the_handler_appends_again() =>
        await AssertionFailsFor(specification => specification with
        {
            GivenEvents = [new(specification.WhenAppended.EventContract, specification.WhenAppended.Values)]
        });

    [Fact]
    async Task should_treat_the_first_record_of_a_key_as_added_despite_records_at_other_keys()
    {
        ChangeCapture(new(SemanticCaptureConditionKind.Added, []), specification => specification with
        {
            GivenEvents = [new(specification.WhenAppended.EventContract, [.. specification.WhenAppended.Values.Select(value => value with { Value = new SemanticTextValue(OtherKey) })])]
        });
        Render(clientOnly: true);
        await RunGenerated("when_receiving_a_shipment");
    }

    [Fact]
    async Task should_decide_added_from_foreign_capture_state_not_private_output_history()
    {
        ChangeCapture(new(SemanticCaptureConditionKind.Added, []), specification => specification with { GivenEvents = specification.ThenEvents });
        Render(clientOnly: true);
        await RunGenerated("when_receiving_a_shipment");
    }

    [Fact]
    async Task should_key_capture_state_by_the_capture_key_not_the_foreign_stream()
    {
        ChangeCapture(new(SemanticCaptureConditionKind.Added, []), recovery: true, change: specification => specification with
        {
            GivenEvents = [new(specification.WhenAppended.EventContract, specification.WhenAppended.Values) { EventSource = new(SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.Text), new SemanticTextValue("another-stream")) }],
            ThenEvents = [],
            ThenNoEvents = true
        });
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
                    EventSource = expected.EventSource with { Value = new SemanticTextValue("11111111-1111-1111-1111-111111111111") }
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

    void ChangeCapture(SemanticCaptureCondition? when, Func<SemanticSpecification, SemanticSpecification> change) => ChangeCapture(when, recovery: false, change);

    void ChangeCapture(SemanticCaptureCondition? when, bool recovery, Func<SemanticSpecification, SemanticSpecification> change)
    {
        SemanticSlice Changed(SemanticSlice slice) => slice.Name != "TrackShipments" ? slice : slice with
        {
            Captures = [.. slice.Captures.Select(capture => capture with { Appends = [.. capture.Appends.Select(append => append with { When = when })] })],
            Specifications = [.. slice.Specifications.Select(change)]
        };
        _model = Transform(_model, Changed, recovery);
    }

    async Task AssertionFailsFor(Func<SemanticSpecification, SemanticSpecification> change)
    {
        ChangeCapture(null, change);
        await AssertionFails("when_receiving_a_shipment");
    }

    void ChangePublication(Func<SemanticSpecification, SemanticSpecification> change, bool recovery = false) =>
        _model = Transform(_model, slice => slice.Name == "PublishOrderShipped" ? slice with { Specifications = [.. slice.Specifications.Select(change)] } : slice, recovery);

    async Task AssertionFails(string name)
    {
        Render(clientOnly: true);
        var exception = await Assert.ThrowsAsync<TargetInvocationException>(() => RunGenerated(name));
        exception.InnerException.GetType().Namespace.ShouldEqual("Xunit.Sdk");
    }
}
#endif
