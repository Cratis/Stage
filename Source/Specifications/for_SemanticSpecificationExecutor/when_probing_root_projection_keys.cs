// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Testing;
using Cratis.Chronicle.Testing.ReadModels;
using Cratis.Stage.Specifications.Types;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor;

public class when_probing_root_projection_keys : Specification
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void should_route_two_streams_using_the_fluent_key(bool constant)
    {
        using var scenario = new ReadModelScenario<Totals>(null, new Defaults(new SemanticClientArtifactsProvider([typeof(Recorded)])))
            .WithProjection(builder =>
            {
                builder.NoAutoMap();
                builder.From<Recorded>(from =>
                {
                    if (constant) from.UsingConstantKey("global");
                    else from.UsingKey(evt => evt.Sku);
                    from.Add(model => model.Count).With(evt => evt.Quantity);
                });
            });
        scenario.CollectEventsFor(new EventSourceId("first"), [new Recorded("global", 2)]);
        scenario.CollectEventsFor(new EventSourceId("second"), [new Recorded("global", 3)]);
        var instance = Assert.Single(scenario.Instances);
        instance.Key.ToString().ShouldEqual("global");
        instance.Value.Count.ShouldEqual(5);
    }

    [Theory]
    [InlineData(true, 1)]
    [InlineData(false, -1)]
    public void should_start_optional_counters_from_zero(bool increment, int expected)
    {
        using var scenario = new ReadModelScenario<Totals>(null, new Defaults(new SemanticClientArtifactsProvider([typeof(Recorded)])))
            .WithProjection(builder =>
            {
                builder.NoAutoMap();
                builder.From<Recorded>(from =>
                {
                    from.UsingConstantKey("global");
                    if (increment) from.Increment(model => model.Optional);
                    else from.Decrement(model => model.Optional);
                });
            });
        scenario.CollectEventsFor(new EventSourceId("first"), [new Recorded("global", 2)]);
        Assert.Single(scenario.Instances).Value.Optional.ShouldEqual(expected);
    }

    [EventType]
    public record Recorded(string Sku, int Quantity);

    public class Totals
    {
        public int Count { get; set; }

        public int? Optional { get; set; }
    }
}
