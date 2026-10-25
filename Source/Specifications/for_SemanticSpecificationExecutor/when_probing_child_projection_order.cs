// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Testing;
using Cratis.Chronicle.Testing.ReadModels;
using Cratis.Stage.Specifications.Types;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor;

public class when_probing_child_projection_order : Specification
{
    [Fact]
    public void should_keep_first_insertion_order_through_updates_joins_and_removals()
    {
        using var scenario = new ReadModelScenario<Parent>(null, new Defaults(new SemanticClientArtifactsProvider([typeof(Opened), typeof(Placed), typeof(Labeled), typeof(Removed)])))
            .WithProjection(builder =>
            {
                builder.NoAutoMap();
                builder.From<Opened>(from => from.Set(model => model.Name).To(evt => evt.ParentName));
                builder.Children(model => model.Children, children =>
                {
                    children.NoAutoMap();
                    children.IdentifiedBy(child => child.Code);
                    children.From<Placed>(from =>
                    {
                        from.UsingKey(evt => evt.ChildCode);
                        from.UsingParentKeyFromContext(context => context.EventSourceId);
                        from.Set(child => child.Code).To(evt => evt.ChildCode);
                        from.Set(child => child.Quantity).To(evt => evt.Amount);
                    });
                    children.Join<Labeled>(join => join.Set(child => child.Label).To(evt => evt.Text));
                    children.RemovedWith<Removed>(removed =>
                    {
                        removed.UsingKey(evt => evt.ChildCode);
                        removed.UsingParentKeyFromContext(context => context.EventSourceId);
                    });
                });
            });
        var parent = new EventSourceId("parent");
        scenario.CollectEventsFor(parent, [new Opened("parent"), new Placed("second", 2), new Placed("first", 1)]);
        Child[] children = [.. Assert.Single(scenario.Instances).Value.Children];
        Assert.Equal(["second", "first"], [.. children.Select(child => child.Code)]);
        scenario.CollectEventsFor(parent, [new Placed("second", 3)]);
        children = [.. Assert.Single(scenario.Instances).Value.Children];
        Assert.Equal(["second", "first"], [.. children.Select(child => child.Code)]);
        children[0].Quantity.ShouldEqual(3);
        scenario.CollectEventsFor(new EventSourceId("second"), [new Labeled("Second label")]);
        children = [.. Assert.Single(scenario.Instances).Value.Children];
        Assert.Equal(["second", "first"], [.. children.Select(child => child.Code)]);
        children[0].Label.ShouldEqual("Second label");
        scenario.CollectEventsFor(parent, [new Removed("second")]);
        Assert.Single(Assert.Single(scenario.Instances).Value.Children).Code.ShouldEqual("first");
    }

    [EventType]
    public record Opened(string ParentName);

    [EventType]
    public record Placed(string ChildCode, int Amount);

    [EventType]
    public record Labeled(string Text);

    [EventType]
    public record Removed(string ChildCode);

    public class Parent
    {
        public string Name { get; set; } = string.Empty;

        public IEnumerable<Child> Children { get; set; } = [];
    }

    public class Child
    {
        public string Code { get; set; } = string.Empty;

        public int Quantity { get; set; }

        public string Label { get; set; } = string.Empty;
    }
}
