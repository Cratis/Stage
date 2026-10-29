// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Serialization;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_reading_capture_state;

public class with_a_rendered_source_key_specification : given.a_translation_specification_application
{
    [Fact]
    async Task should_pass_when_the_explicit_source_is_the_capture_key()
    {
        SetSource(_orderId);
        Render(clientOnly: true);
        await RunGenerated("when_receiving_a_shipment");
    }

    [Fact]
    async Task should_reject_a_source_that_is_not_the_capture_key()
    {
        SetSource(new SemanticTextValue("9fa85f64-5717-4562-b3fc-2c963f66afa9"));
        Render(clientOnly: true);
        var exception = await Catch.Exception(() => RunGenerated("when_receiving_a_shipment"));
        exception.ShouldNotBeNull();
        exception.GetType().Name.ShouldEqual("CaptureKeyIsNotTheEventSource");
    }

    void SetSource(SemanticValue sourceId)
    {
        _model = Transform(SemanticModelSerializer.Deserialize(EsmGoldenVectors.Get(33).Bytes.AsSpan()), slice =>
        {
            if (slice.Kind != SemanticSliceKind.Translate) return slice;
            var source = slice.Events.Single(contract => contract.Origin is not null);
            var target = slice.Events.Single(contract => contract.Origin is null);
            var keyType = source.Properties.Single().Type;
            return slice with
            {
                Specifications = [slice.Specifications.Single() with
                {
                    Name = "ReceivingAShipment",
                    GivenEvents = [],
                    ThenReadModels = [],
                    ThenEvents = [new(target.Id, [new(target.Properties.Single().Id, _orderId)]) { EventSource = new(keyType, _orderId) }],
                    WhenAppended = new(source.Id, [new(source.Properties.Single().Id, _orderId)]) { EventSource = new(SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.Text), sourceId) }
                }]
            };
        });
    }
}
#endif
