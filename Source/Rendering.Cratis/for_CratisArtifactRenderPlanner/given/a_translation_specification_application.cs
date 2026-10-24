// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Reflection;
using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Serialization;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;

public class a_translation_specification_application : a_translation_application
{
    protected static readonly SemanticValue _orderId = new SemanticTextValue("3fa85f64-5717-4562-b3fc-2c963f66afa6");

    void Establish()
    {
        _model = Transform(SemanticModelSerializer.Deserialize(PublicEventsCorpus.V9.EsmBytes.ToArray()), slice =>
        {
            if (slice.Name != "TrackShipments") return slice;
            var source = slice.Events.Single(contract => contract.Name == "ShipmentDispatched");
            var target = slice.Events.Single(contract => contract.Name == "OrderDispatched");
            var property = source.Properties.Single();
            return slice with
            {
                Specifications = [new(
                    SemanticId.Create(SemanticKind.Specification, "receive-shipment"),
                    "ReceivingAShipment",
                    [],
                    [],
                    null,
                    [new(target.Id, [new(target.Properties.Single().Id, _orderId)]) { EventSource = new(SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.Text), _orderId) }],
                    [],
                    [],
                    [])
                {
                    WhenAppended = new(source.Id, [new(property.Id, _orderId)])
                }]
            };
        });
    }

    protected async Task RunGenerated(string name)
    {
        var type = _assembly.GetTypes().Single(type => type.Name == name);
        var instance = Activator.CreateInstance(type)!;
        type.GetMethod("Establish", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(instance, null);
        await (Task)type.GetMethod("Because", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(instance, null)!;
        var facts = type.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance).Where(method => Attribute.IsDefined(method, typeof(FactAttribute))).ToArray();
        facts.ShouldNotBeEmpty();
        foreach (var fact in facts) fact.Invoke(instance, null);
    }
}
#endif
