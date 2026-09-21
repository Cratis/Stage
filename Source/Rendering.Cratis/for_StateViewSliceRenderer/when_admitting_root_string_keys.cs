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
    public void should_preserve_different_effective_literals_and_the_record()
    {
        Compile(Global + "\nfrom OrderUpdated\n  key literal \"other\"\n  total = total");
        var content = Render().Content;
        content.ShouldContain("[FromEvent<OrderCreated>(ConstantKey = \"global\")]");
        content.ShouldContain("[FromEvent<OrderUpdated>(ConstantKey = \"other\")]");
        content.ShouldContain("public record OrderReadModel([SetFrom<OrderCreated>(nameof(OrderCreated.Total))] decimal Total)");
        content.ShouldNotContain("[Key]");
    }

    [Fact]
    public void should_reject_a_partial_inline_property_override()
    {
        Compile(Global.Replace("from OrderCreated", "from OrderCreated key number, OrderUpdated", StringComparison.Ordinal));
        Reject(UnsupportedRootStringKeyReason.MixedKeys).EventName.ShouldEqual("OrderCreated");
    }

    [Fact]
    public void should_leave_a_fully_overridden_literal_inactive()
    {
        Compile(Global.Replace("from OrderCreated", "from OrderCreated key number, OrderUpdated key number", StringComparison.Ordinal));
        var content = Render().Content;
        content.ShouldContain("[FromEvent<OrderCreated>(key: nameof(OrderCreated.Number))]");
        content.ShouldNotContain("ConstantKey");
        content.ShouldContain("OrderById(IReadModels readModels, EventSourceId id)");
    }

    [Theory]
    [InlineData("from OrderCreated\n  total = total", "EventSourceId id")]
    [InlineData("from OrderCreated\n  key number\n  total = total", "EventSourceId number")]
    public void should_leave_shadowed_literals_inactive_and_preserve_original_typing(string first, string parameter)
    {
        Compile(first + "\n" + Global);
        var content = Render().Content;
        content.ShouldNotContain("ConstantKey");
        content.ShouldContain("OrderById(IReadModels readModels, " + parameter + ")");
    }

    [Fact]
    public void should_ignore_shadowed_invalid_keys_after_a_literal_winner()
    {
        Compile(Global + "\nfrom OrderCreated\n  key literal \"bad()\"\n  total = total");
        var content = Render().Content;
        content.ShouldContain("ConstantKey = \"global\"");
        content.ShouldNotContain("bad()");
        content.ShouldNotContain("[Key]");
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
    public void should_leave_non_root_literal_behavior_unchanged(string blocks)
    {
        Compile(blocks);
        var file = Render();
        file.Content.ShouldNotContain("ConstantKey");
        file.Diagnostics.ShouldContain(diagnostic => diagnostic.Contains("event routes on the event source id", StringComparison.Ordinal));
    }

    UnsupportedRootStringKey Reject(UnsupportedRootStringKeyReason reason)
    {
        var failure = Assert.IsType<UnsupportedRootStringKey>(Catch.Exception(() => Render()));
        failure.Reason.ShouldEqual(reason);
        return failure;
    }
}
