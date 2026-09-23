// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Projections;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.Renderers;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_StateViewSliceRenderer;

public class when_admitting_root_string_keys : given.a_root_string_key_slice
{
    [Theory]
    [InlineData("", UnsupportedRootStringKeyReason.EmptyLiteral)]
    [InlineData("a(b)", UnsupportedRootStringKeyReason.UnencodableLiteral)]
    [InlineData("$value(global)", UnsupportedRootStringKeyReason.UnencodableLiteral)]
    [InlineData("a\"b", UnsupportedRootStringKeyReason.UnencodableLiteral)]
    [InlineData("a\\b", UnsupportedRootStringKeyReason.UnencodableLiteral)]
    [InlineData("a\nb", UnsupportedRootStringKeyReason.UnencodableLiteral)]
    [InlineData("a\rb", UnsupportedRootStringKeyReason.UnencodableLiteral)]
    [InlineData("a\tb", UnsupportedRootStringKeyReason.UnencodableLiteral)]
    public void should_reject_payloads_outside_the_exact_grammar(string value, UnsupportedRootStringKeyReason reason)
    {
        Compile(Global);
        var projection = _slice.Projections.Single();
        var from = projection.Blocks.OfType<FromSyntax>().Single();
        var key = (ExpressionKeySyntax)from.Key!;
        var literal = (LiteralExpressionSyntax)key.Expression;
        var location = literal.Location with { Path = "./Models/../Orders.play", Line = 37, Column = 13 };
        _slice = _slice with { Projections = [projection with { Blocks = [@from with { Key = key with { Expression = literal with { Value = value, Location = location } } }] }] };
        var failure = Reject(reason);
        failure.Location.ShouldEqual(location);
        failure.SlicePath.ShouldEqual("Sales.Orders.Summary");
        failure.ProjectionName.ShouldEqual("Order");
        failure.ReadModel.ShouldEqual("OrderReadModel");
        failure.EventName.ShouldEqual("OrderCreated");
        failure.Message.ShouldContain("STAGE-CRATIS-KEY-002");
    }

    [Theory]
    [InlineData("from OrderUpdated\n  total = total")]
    [InlineData("from OrderUpdated\n  key number\n  total = total")]
    [InlineData("from OrderUpdated\n  key literal 42\n  total = total")]
    public void should_reject_mixed_effective_keys(string other)
    {
        Compile(Global + "\n" + other);
        Reject(UnsupportedRootStringKeyReason.MixedKeys).EventName.ShouldEqual("OrderUpdated");
    }

    [Fact]
    public void should_keep_the_composite_guard_priority()
    {
        Compile(Global + "\nfrom OrderUpdated\n  key OrderKey\n    number = number\n  total = total");
        Assert.IsType<UnsupportedCompositeProjectionKey>(Catch.Exception(() => Render()));
    }

    [Fact]
    public void should_reject_separate_projection_identity()
    {
        Compile(Global, projectionKey: "key number");
        Reject(UnsupportedRootStringKeyReason.ExplicitIdentity).Location.ShouldEqual(_slice.Projections.Single().Key!.Location);
    }

    [Theory]
    [InlineData("id = number")]
    [InlineData("Id = total")]
    [InlineData("ID = literal true")]
    [InlineData("nested id\n  from OrderUpdated\n    total = total")]
    [InlineData("children id identified by number\n  from OrderUpdated\n    number = number")]
    public void should_reject_conventional_identity_from_actual_inference(string mapping)
    {
        Compile(mapping.StartsWith("id =", StringComparison.Ordinal) || mapping.StartsWith("Id =", StringComparison.Ordinal) || mapping.StartsWith("ID =", StringComparison.Ordinal)
            ? Global + "\n  " + mapping
            : Global + "\n" + mapping);
        Reject(UnsupportedRootStringKeyReason.ConventionalId).Location.ShouldEqual(_slice.Projections.Single().Location);
    }

    [Fact]
    public void should_reject_an_affected_parent()
    {
        Compile(Global.Replace("  total = total", "  parent number\n  total = total", StringComparison.Ordinal));
        Reject(UnsupportedRootStringKeyReason.RootParent).Location.ShouldEqual(_slice.Projections.Single().Blocks.OfType<FromSyntax>().Single().ParentKey!.Location);
    }

    [Theory]
    [InlineData("Uuid")]
    [InlineData("LookupKey")]
    [InlineData("Int")]
    [InlineData("String[]")]
    [InlineData("String?")]
    public void should_reject_incompatible_owned_identifying_types(string type)
    {
        Compile(Global, "query OrderById => OrderReadModel\n  by lookup " + type);
        Reject(UnsupportedRootStringKeyReason.IncompatibleByType).Location.ShouldEqual(_slice.Queries.Single().By!.Location);
    }

    [Fact]
    public void should_ignore_foreign_identifying_types()
    {
        Compile(Global, "query OtherById => OtherModel\n  by lookup Uuid");
        Render().Content.ShouldContain("OrderReadModelById(IReadModels readModels, EventSourceId id)");
    }

    [Fact]
    public void should_admit_queries_before_the_profile_even_for_foreign_queries()
    {
        Compile(Global, "query Other => OtherModel[]\n  filter number String", "key number");
        Assert.IsType<UnsupportedQueryIntent>(Catch.Exception(() => Render()));
    }

    [Fact]
    public void should_reject_duplicate_mappings_in_different_literal_subscriptions()
    {
        Compile(Global + "\nfrom OrderUpdated\n  key literal \"other\"\n  total = total");
        Assert.IsType<UnsupportedLegacyProjection>(Catch.Exception(() => Render()));
    }

    [Fact]
    public void should_reject_a_partial_inline_property_override()
    {
        Compile(Global.Replace("from OrderCreated", "from OrderCreated key number, OrderUpdated", StringComparison.Ordinal));
        Assert.IsType<UnsupportedLegacyProjection>(Catch.Exception(() => Render()));
    }

    [Fact]
    public void should_reject_multi_event_mappings_even_when_literal_keys_are_overridden()
    {
        Compile(Global.Replace("from OrderCreated", "from OrderCreated key number, OrderUpdated key number", StringComparison.Ordinal));
        Assert.IsType<UnsupportedLegacyProjection>(Catch.Exception(() => Render()));
    }

    [Theory]
    [InlineData("from OrderCreated\n  total = total")]
    [InlineData("from OrderCreated\n  key number\n  total = total")]
    public void should_reject_shadowed_literal_mappings_that_would_be_dropped(string first)
    {
        Compile(first + "\n" + Global);
        Assert.IsType<UnsupportedLegacyProjection>(Catch.Exception(() => Render()));
    }

    [Fact]
    public void should_reject_shadowed_invalid_keys_with_duplicate_mappings()
    {
        Compile(Global + "\nfrom OrderCreated\n  key literal \"bad()\"\n  total = total");
        Assert.IsType<UnsupportedLegacyProjection>(Catch.Exception(() => Render()));
    }

    [Fact]
    public void should_only_admit_the_first_projection()
    {
        Compile(Global, projectionKey: "key number");
        var blocked = _slice.Projections.Single();
        Compile("from OrderCreated\n  total = total");
        var safe = _slice.Projections.Single();
        _slice = _slice with { Projections = [safe, blocked with { Name = "Ignored" }] };
        Render().Content.ShouldNotContain("ConstantKey");
        _slice = _slice with { Projections = [blocked, safe with { Name = "Ignored" }] };
        Reject(UnsupportedRootStringKeyReason.ExplicitIdentity);
    }

    [Theory]
    [InlineData("nested details\n  from OrderCreated\n    key literal \"global\"\n    total = total")]
    [InlineData("children lines identified by number\n  from OrderCreated\n    key literal \"global\"\n    number = number")]
    public void should_reject_non_root_literal_keys_that_would_change_routing(string blocks)
    {
        Compile(blocks);
        Assert.IsType<UnsupportedLegacyProjection>(Catch.Exception(() => Render()));
    }

    UnsupportedRootStringKey Reject(UnsupportedRootStringKeyReason reason)
    {
        var failure = Assert.IsType<UnsupportedRootStringKey>(Catch.Exception(() => Render()));
        failure.Reason.ShouldEqual(reason);
        return failure;
    }
}
