// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Testing;
using Cratis.Chronicle.Testing.ReadModels;
using Cratis.Stage.Specifications.Types;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor;

public class when_probing_composite_projection_values : Specification
{
    Order _order = null!;

    void Because()
    {
        using var scenario = new ReadModelScenario<Order>(null, new Defaults(new SemanticClientArtifactsProvider([typeof(CompositeOrderPlaced)])))
            .WithProjection(builder =>
            {
                builder.NoAutoMap();
                builder.From<CompositeOrderPlaced>(from =>
                {
                    from.Set(new PropertyPath(nameof(Order.ShipTo))).To(new PropertyPath(nameof(CompositeOrderPlaced.ShipTo)));
                    from.Set(new PropertyPath(nameof(Order.Lines))).To(new PropertyPath(nameof(CompositeOrderPlaced.Lines)));
                    from.Set(new PropertyPath(nameof(Order.NoLines))).To(new PropertyPath(nameof(CompositeOrderPlaced.NoLines)));
                    from.Set(new PropertyPath(nameof(Order.City))).To(new PropertyPath($"{nameof(CompositeOrderPlaced.ShipTo)}.{nameof(Address.City)}"));
                });
            });
        scenario.CollectEventsFor(
            new EventSourceId("one"),
            [new CompositeOrderPlaced(
                Parse("""{"street":"Main 1","city":"Oslo","postalCode":"0150","country":"NO","reference":"123e4567-e89b-12d3-a456-426614174000"}"""),
                Parse("""[{"sku":"A-1","quantity":2,"unitPrice":49},{"sku":"B-2","quantity":1,"unitPrice":12345678901234567.89}]"""),
                Parse("[]"))]);
        _order = Assert.Single(scenario.Instances).Value;
    }

    [Fact] void should_keep_text_that_looks_numeric_or_like_a_guid() => (_order.ShipTo.PostalCode, _order.ShipTo.Reference).ShouldEqual(("0150", "123e4567-e89b-12d3-a456-426614174000"));
    [Fact] void should_keep_the_other_composite_members() => (_order.ShipTo.Street, _order.ShipTo.City, _order.ShipTo.Country).ShouldEqual(("Main 1", "Oslo", "NO"));
    [Fact] void should_keep_collection_order_and_double_exact_numbers() => _order.Lines.Select(line => (line.Sku, line.Quantity)).ToArray().ShouldEqual([("A-1", 2), ("B-2", 1)]);
    [Fact] void should_keep_a_double_exact_decimal() => _order.Lines[0].UnitPrice.ShouldEqual(49m);
    [Fact] void should_lose_a_high_precision_decimal_because_unknown_schema_numbers_become_double() => _order.Lines[1].UnitPrice.ShouldNotEqual(12345678901234567.89m);
    [Fact] void should_keep_an_empty_collection_empty() => _order.NoLines.ShouldBeEmpty();
    [Fact] void should_read_a_scalar_through_a_nested_event_path() => _order.City.ShouldEqual("Oslo");

    static JsonElement Parse(string json) => JsonSerializer.Deserialize<JsonElement>(json);

    [EventType]
    public record CompositeOrderPlaced(JsonElement ShipTo, JsonElement Lines, JsonElement NoLines);

    public class Address
    {
        public string Street { get; set; } = string.Empty;

        public string City { get; set; } = string.Empty;

        public string PostalCode { get; set; } = string.Empty;

        public string Country { get; set; } = string.Empty;

        public string Reference { get; set; } = string.Empty;
    }

    public class Line
    {
        public string Sku { get; set; } = string.Empty;

        public int Quantity { get; set; }

        public decimal UnitPrice { get; set; }
    }

    public class Order
    {
        public Address ShipTo { get; set; } = new();

        public Line[] Lines { get; set; } = [];

        public Line[] NoLines { get; set; } = [];

        public string City { get; set; } = string.Empty;
    }
}
