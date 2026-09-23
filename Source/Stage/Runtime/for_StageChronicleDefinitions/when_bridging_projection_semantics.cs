// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Cratis.Stage.Contracts;
using Cratis.Stage.Contracts.Projections;
using Xunit;

namespace Cratis.Stage.Runtime.for_StageChronicleDefinitions;

public class when_bridging_projection_semantics : Specification
{
    [Fact]
    public void should_preserve_all_event_subscriptions_and_recursive_scalar_nested_definitions()
    {
        var model = EventModelLoader.LoadFromSource("""
            module Catalog
              feature Items
                slice StateView Summary
                  projection Summary => SummaryModel
                    all
                      count total
                    nested shipping
                      from Shipped
                        city = city
                      nested carrier
                        from CarrierAssigned
                          name = name
            """);

        var definition = Assert.Single(StageChronicleDefinitions.Build(model, "event-log").Projections);
        Assert.True(definition.SubscribesToAllEvents);
        var shipping = Assert.Single(definition.Nested).Value;
        Assert.Equal("*NotSet*", shipping.IdentifiedBy);
        var carrier = Assert.Single(shipping.Nested).Value;
        Assert.Equal("*NotSet*", carrier.IdentifiedBy);
        Assert.Equal("name", Assert.Single(carrier.From).Value.Properties["name"]);
    }

    [Fact]
    public void should_translate_visitor_expressions_at_the_runtime_boundary_recursively()
    {
        var model = EventModelLoader.LoadFromSource("""
            module Catalog
              feature Items
                slice StateView Summary
                  projection Summary => SummaryModel
                    from ItemRegistered key id
                      id = id
            """);
        var slice = model.Collections.Single().Modules.Single().Features.Single().Slices.Single();
        var readModel = slice.ReadModel!;
        var projection = readModel.Projection!;
        var expressions = new FromDefinition(
            [new("cause", "$causedBy(name)"), new("plain", "$causedBy"),
                new("quoted", "\"$causedBy(name)\""), new("template", "`by ${$causedBy(name)}`")],
            "$composite(OrderKey, id=$causedBy(name), region=$value(us))",
            "$causedBy(subject)");
        var child = new ChildrenDefinition(
            "$causedBy(subject)",
            new Dictionary<string, FromDefinition> { ["ChildAdded"] = expressions },
            new Dictionary<string, JoinDefinition> { ["ChildJoined"] = new("id", [new("name", "$causedBy(name)")], "$causedBy") },
            new Dictionary<string, ChildrenDefinition>(),
            new([new("cause", "$causedBy")]),
            null,
            new Dictionary<string, RemovedWithDefinition> { ["ChildRemoved"] = new("$causedBy(name)", "$causedBy") },
            new Dictionary<string, RemovedWithJoinDefinition> { ["ChildDetached"] = new("$causedBy(subject)") });
        var converted = projection with
        {
            From = new Dictionary<string, FromDefinition> { ["ItemRegistered"] = expressions },
            Children = new Dictionary<string, ChildrenDefinition> { ["lines"] = child },
            Nested = new Dictionary<string, ChildrenDefinition> { ["shipping"] = child },
            FromEvery = new([new("cause", "$causedBy(name)")]),
            RemovedWith = new Dictionary<string, RemovedWithDefinition> { ["ItemRemoved"] = new("$causedBy(name)", "$causedBy") },
            RemovedWithJoin = new Dictionary<string, RemovedWithJoinDefinition> { ["ItemDetached"] = new("$causedBy(subject)") }
        };
        var definition = Assert.Single(StageChronicleDefinitions.Build(WithProjection(model, slice, readModel with { Projection = converted }), "event-log").Projections);
        Assert.Equal("$composite(id=$eventContext(causedBy.name), region=$value(us))", Assert.Single(definition.From).Value.Key);
        Assert.Equal("$eventContext(causedBy.subject)", Assert.Single(definition.From).Value.ParentKey);
        Assert.Equal("$eventContext(causedBy.name)", Assert.Single(definition.From).Value.Properties["cause"]);
        Assert.Equal("$eventContext(causedBy)", Assert.Single(definition.From).Value.Properties["plain"]);
        Assert.Equal("\"$causedBy(name)\"", Assert.Single(definition.From).Value.Properties["quoted"]);
        Assert.Equal("`by ${$eventContext(causedBy.name)}`", Assert.Single(definition.From).Value.Properties["template"]);
        Assert.Equal("$eventContext(causedBy.name)", definition.All.Properties["cause"]);
        Assert.Equal("$eventContext(causedBy.name)", Assert.Single(definition.RemovedWith).Value.Key);
        Assert.Equal("$eventContext(causedBy)", Assert.Single(definition.RemovedWith).Value.ParentKey);
        Assert.Equal("$eventContext(causedBy.subject)", Assert.Single(definition.RemovedWithJoin).Value.Key);
        var nested = Assert.Single(definition.Nested).Value;
        Assert.Equal("$eventContext(causedBy.subject)", nested.IdentifiedBy);
        Assert.Equal("$eventContext(causedBy)", Assert.Single(nested.Join).Value.Key);
        Assert.Equal("$eventContext(causedBy.name)", Assert.Single(nested.Join).Value.Properties["name"]);
        Assert.Equal("$eventContext(causedBy.name)", Assert.Single(nested.RemovedWith).Value.Key);
        Assert.Equal("$eventContext(causedBy.subject)", Assert.Single(nested.RemovedWithJoin).Value.Key);
        Assert.Equal("$eventContext(causedBy.name)", Assert.Single(definition.Children).Value.From.Single().Value.Properties["cause"]);
    }

    [Fact]
    public void should_keep_native_composite_keys_and_their_parts() =>
        Assert.Equal("$composite(a=$eventContext(causedBy.name), b=$value(us))",
            ProjectionRuntimeKey.Translate("$composite(a=$causedBy(name), b=$value(us))"));

    [Theory]
    [InlineData("$composite(Type, region=$value(us-east), id=id)")]
    [InlineData("$composite(region=$value(us-east), id=id)")]
    [InlineData("$composite(Type, region=`${code}`, id=id)")]
    [InlineData("$composite(Type, amount=-1, id=id)")]
    [InlineData("$value(not=key)")]
    [InlineData("$value(not@key)")]
    [InlineData("$causedBy(name1)")]
    [InlineData("$causedBy(first_name)")]
    [InlineData("$eventContext(causedBy.name1)")]
    [InlineData("$eventContext(causedBy.first_name)")]
    public void should_reject_keys_that_only_partially_match_the_pinned_resolvers(string value) =>
        Assert.Throws<UnsupportedProjectionRuntimeExpression>(() => ProjectionRuntimeKey.Translate(value));

    [Theory]
    [InlineData("$value(us-east)")]
    [InlineData("$value(us/east)")]
    [InlineData("$composite(Type, id=id, region=$value(us))")]
    [InlineData("$eventContext(causedBy.subject)")]
    [InlineData("$eventSourceId")]
    [InlineData("invoiceId")]
    public void should_accept_whole_resolvable_keys(string value) =>
        Assert.NotEmpty(ProjectionRuntimeKey.Translate(value));

    [Theory]
    [InlineData("$composite(Type, a=\"x,y\")")]
    [InlineData("$causedBy()")]
    [InlineData("$causedBy.name")]
    [InlineData("`unfinished ${$causedBy(name)`")]
    public void should_reject_ambiguous_runtime_expressions(string value) =>
        Assert.Throws<UnsupportedProjectionRuntimeExpression>(() => ProjectionRuntimeExpression.Translate(value));

    [Fact]
    public void should_reject_versioned_declared_event_registration()
    {
        var model = EventModelLoader.LoadFromSource("""
            module Catalog
              feature Items
                slice StateChange Register
                  event ItemRegistered
                    name String
            """);
        var slice = model.Collections.Single().Modules.Single().Features.Single().Slices.Single();
        var changed = slice with { Events = [slice.Events.Single() with { Name = "ItemRegistered+2" }] };
        Assert.Throws<UnsupportedProjectionEventType>(() => StageChronicleDefinitions.BuildEventTypes(WithSlice(model, changed)));
    }

    [Fact]
    public void should_reject_event_aliases_that_canonicalize_to_the_same_identity()
    {
        var model = EventModelLoader.LoadFromSource("""
            module Catalog
              feature Items
                slice StateView Summary
                  projection Summary => SummaryModel
                    from ItemRegistered
                      name = name
            """);
        var slice = model.Collections.Single().Modules.Single().Features.Single().Slices.Single();
        var readModel = slice.ReadModel!;
        var projection = readModel.Projection!;
        var from = projection.From.Single().Value;
        var changed = projection with { From = new Dictionary<string, FromDefinition>
        {
            ["ItemRegistered"] = from,
            ["ItemRegistered+1"] = from
        } };
        Assert.Throws<UnsupportedProjectionEventType>(() => StageChronicleDefinitions.Build(
            WithProjection(model, slice, readModel with { Projection = changed }), "event-log"));
    }

    [Fact]
    public void should_parse_versioned_event_type_keys_instead_of_storing_the_whole_name_as_an_id()
    {
        var model = EventModelLoader.LoadFromSource("""
            module Catalog
              feature Items
                slice StateView Summary
                  projection Summary => SummaryModel
                    from ItemRegistered
                      name = name
            """);
        var slice = model.Collections.Single().Modules.Single().Features.Single().Slices.Single();
        var readModel = slice.ReadModel!;
        var projection = readModel.Projection!;
        var source = projection.From.Single();
        var changed = projection with { From = new Dictionary<string, Contracts.Projections.FromDefinition>
        {
            ["ItemRegistered+2"] = source.Value
        } };
        var definition = Assert.Single(StageChronicleDefinitions.Build(WithProjection(model, slice, readModel with { Projection = changed }), "event-log").Projections);
        var eventType = Assert.Single(definition.From).Key;
        Assert.Equal("ItemRegistered", eventType.Id);
        Assert.Equal(2u, eventType.Generation);
    }

    static EventModel WithProjection(EventModel model, Slice slice, ReadModelDefinition readModel) =>
        WithSlice(model, slice with { ReadModel = readModel });

    static EventModel WithSlice(EventModel model, Slice slice) => model with
    {
        Collections = [model.Collections.Single() with
        {
            Modules = [model.Collections.Single().Modules.Single() with
            {
                Features = [model.Collections.Single().Modules.Single().Features.Single() with { Slices = [slice] }]
            }]
        }]
    };
}
#endif
