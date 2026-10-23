// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Serialization;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Testing;
using Cratis.Chronicle.Testing.ReadModels;
using Cratis.Stage.Specifications.Types;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor;

public class when_probing_schema_typed_composite_event_values : Specification
{
    Order _order = null!;

    void Because()
    {
        using var scenario = new ReadModelScenario<Order>(null, new Defaults(new SemanticClientArtifactsProvider([typeof(TypedOrderPlaced)])))
            .WithProjection(builder =>
            {
                builder.NoAutoMap();
                builder.From<TypedOrderPlaced>(from =>
                {
                    from.Set(new PropertyPath(nameof(Order.ShipTo))).To(new PropertyPath(nameof(TypedOrderPlaced.ShipTo)));
                    from.Set(new PropertyPath(nameof(Order.Lines))).To(new PropertyPath(nameof(TypedOrderPlaced.Lines)));
                    from.Set(new PropertyPath(nameof(Order.City))).To(new PropertyPath($"{nameof(TypedOrderPlaced.ShipTo)}.{nameof(Address.City)}"));
                });
            });
        scenario.CollectEventsFor(
            new EventSourceId("one"),
            [new TypedOrderPlaced(
                new ShipToValue { Value = Parse("""{"city":"Oslo","postalCode":"0150","reference":"123e4567-e89b-12d3-a456-426614174000"}""") },
                new LinesValue { Value = Parse("""[{"sku":"A-1","quantity":9007199254740993,"unitPrice":12345678901234567.89},{"sku":"B-2"}]""") })]);
        _order = Assert.Single(scenario.Instances).Value;
    }

    [Fact] void should_keep_an_exact_decimal() => _order.Lines[0].UnitPrice.ShouldEqual(12345678901234567.89m);
    [Fact] void should_keep_an_exact_whole_number_beyond_two_to_the_53() => _order.Lines[0].Quantity.ShouldEqual(9007199254740993L);
    [Fact] void should_keep_text_as_text() => (_order.ShipTo.PostalCode, _order.ShipTo.Reference).ShouldEqual(("0150", "123e4567-e89b-12d3-a456-426614174000"));
    [Fact] void should_not_throw_for_an_omitted_optional_member() => _order.Lines[1].Sku.ShouldEqual("B-2");
    [Fact] void should_read_a_scalar_through_a_nested_event_path() => _order.City.ShouldEqual("Oslo");

    static JsonElement Parse(string json) => JsonSerializer.Deserialize<JsonElement>(json);

    [EventType]
    public record TypedOrderPlaced(ShipToValue ShipTo, LinesValue Lines);

    [JsonConverter(typeof(SemanticStructuredEventValueConverterFactory))]
    [JsonSchemaType(typeof(AddressShape))]
    public sealed class ShipToValue : SemanticStructuredEventValue;

    [JsonConverter(typeof(SemanticStructuredEventValueConverterFactory))]
    [JsonSchemaType(typeof(LineShape[]))]
    public sealed class LinesValue : SemanticStructuredEventValue;

    public class AddressShape
    {
        public string? City { get; set; }

        public string? PostalCode { get; set; }

        public string? Reference { get; set; }
    }

    public class LineShape
    {
        public string? Sku { get; set; }

        public decimal? Quantity { get; set; }

        public decimal? UnitPrice { get; set; }
    }

    public class Address
    {
        public string City { get; set; } = string.Empty;

        public string PostalCode { get; set; } = string.Empty;

        public string Reference { get; set; } = string.Empty;
    }

    public class Line
    {
        public string Sku { get; set; } = string.Empty;

        public long Quantity { get; set; }

        public decimal UnitPrice { get; set; }
    }

    public class Order
    {
        public Address ShipTo { get; set; } = new();

        public Line[] Lines { get; set; } = [];

        public string City { get; set; } = string.Empty;
    }
}
