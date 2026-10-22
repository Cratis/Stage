// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
                new Address { Street = "Main 1", City = "Oslo", PostalCode = "0150", Country = "NO", Reference = "123e4567-e89b-12d3-a456-426614174000" },
                [new Line { Sku = "A-1", Quantity = 2, UnitPrice = 49m }, new Line { Sku = "B-2", Quantity = 1, UnitPrice = 12.5m }],
                [])]);
        _order = Assert.Single(scenario.Instances).Value;
    }

    [Fact] void should_keep_text_that_looks_numeric_or_like_a_guid() => (_order.ShipTo.PostalCode, _order.ShipTo.Reference).ShouldEqual(("0150", "123e4567-e89b-12d3-a456-426614174000"));
    [Fact] void should_keep_the_other_composite_members() => (_order.ShipTo.Street, _order.ShipTo.City, _order.ShipTo.Country).ShouldEqual(("Main 1", "Oslo", "NO"));
    [Fact] void should_keep_collection_order_and_exact_numbers() => _order.Lines.Select(line => (line.Sku, line.Quantity, line.UnitPrice)).ToArray().ShouldEqual([("A-1", 2, 49m), ("B-2", 1, 12.5m)]);
    [Fact] void should_keep_an_empty_collection_empty() => _order.NoLines.ShouldBeEmpty();
    [Fact] void should_read_a_scalar_through_a_nested_event_path() => _order.City.ShouldEqual("Oslo");

    [EventType]
    public record CompositeOrderPlaced(Address ShipTo, Line[] Lines, Line[] NoLines);

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
