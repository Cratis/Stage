// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Projections;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.for_CratisRenderer;
using Cratis.Stage.Rendering.Cratis.Renderers;
using Xunit;

using NativeScreenplayCompiler = Cratis.Screenplay.ScreenplayCompiler;

namespace Cratis.Stage.Rendering.Cratis.for_StateViewSliceRenderer;

public class when_admitting_effective_root_composite_keys : Specification
{
    const string Composite = """
        from OrderCreated
          key OrderKey
            customerId = customerId
            orderNumber = orderNumber
          total = total
        """;

    ApplicationSet _context = null!;
    SliceSyntax _slice = null!;

    [Theory]
    [InlineData("from OrderCreated\n  key orderNumber\n  total = total", "[FromEvent<OrderCreated>(key: nameof(OrderCreated.OrderNumber))]")]
    [InlineData("from OrderCreated key orderNumber\n  total = total", "[FromEvent<OrderCreated>(key: nameof(OrderCreated.OrderNumber))]")]
    [InlineData("from OrderCreated\n  total = total", "[FromEvent<OrderCreated>]")]
    public void should_preserve_supported_native_keys_and_the_absent_key_default(string blocks, string attribute)
    {
        Compile(blocks);
        var file = Render();
        file.Content.ShouldContain(attribute);
        file.Diagnostics.ShouldNotContain(diagnostic => diagnostic.Contains("event routes on", StringComparison.Ordinal));
        RenderedOutput.Errors([file]).ShouldBeEmpty();
    }

    [Fact]
    public void should_admit_a_composite_block_when_every_event_has_an_inline_property_key()
    {
        Compile(Composite.Replace("from OrderCreated", "from OrderCreated key orderNumber, OrderUpdated key customerId", StringComparison.Ordinal));
        var file = Render();
        file.Content.ShouldContain("[FromEvent<OrderCreated>(key: nameof(OrderCreated.OrderNumber))]");
        file.Content.ShouldContain("[FromEvent<OrderUpdated>(key: nameof(OrderUpdated.CustomerId))]");
        file.Diagnostics.ShouldNotContain(diagnostic => diagnostic.Contains("event routes on", StringComparison.Ordinal));
        RenderedOutput.Errors([file]).ShouldBeEmpty();
    }

    [Fact]
    public void should_reject_the_event_that_still_uses_the_block_composite()
    {
        Compile(Composite.Replace("from OrderCreated", "from OrderCreated key orderNumber, OrderUpdated", StringComparison.Ordinal));
        var failure = Assert.IsType<UnsupportedCompositeProjectionKey>(Catch.Exception(() => Render()));
        failure.EventName.ShouldEqual("OrderUpdated");
        failure.Location.ShouldEqual(_slice.Projections.Single().Blocks.OfType<FromSyntax>().Single().Key!.Location);
    }

    [Theory]
    [InlineData("from OrderCreated key orderNumber\n  total = total")]
    [InlineData("from OrderCreated\n  total = total")]
    public void should_ignore_a_shadowed_composite_subscription_after_the_first_event_winner(string first)
    {
        Compile(first + "\n" + Composite);
        var file = Render();
        var expected = first.Contains("key orderNumber", StringComparison.Ordinal)
            ? "[FromEvent<OrderCreated>(key: nameof(OrderCreated.OrderNumber))]"
            : "[FromEvent<OrderCreated>]";
        file.Content.ShouldContain(expected);
        file.Content.Split("[FromEvent<OrderCreated>", StringSplitOptions.None).Length.ShouldEqual(2);
        file.Diagnostics.ShouldNotContain(diagnostic => diagnostic.Contains("event routes on", StringComparison.Ordinal));
        RenderedOutput.Errors([file]).ShouldBeEmpty();
    }

    [Fact]
    public void should_reject_a_composite_first_winner_even_when_a_later_subscription_is_safe()
    {
        Compile(Composite + "\nfrom OrderCreated key orderNumber\n  total = total");
        var failure = Assert.IsType<UnsupportedCompositeProjectionKey>(Catch.Exception(() => Render()));
        failure.EventName.ShouldEqual("OrderCreated");
        failure.Location.ShouldEqual(_slice.Projections.Single().Blocks.OfType<FromSyntax>().First().Key!.Location);
    }

    [Fact]
    public void should_still_reject_an_unshadowed_event_in_a_later_composite_block()
    {
        Compile("from OrderCreated key orderNumber\n  total = total\n" + Composite.Replace("from OrderCreated", "from OrderCreated, OrderUpdated", StringComparison.Ordinal));
        var failure = Assert.IsType<UnsupportedCompositeProjectionKey>(Catch.Exception(() => Render()));
        failure.EventName.ShouldEqual("OrderUpdated");
    }

    [Fact]
    public void should_ignore_a_composite_in_a_projection_that_is_not_rendered()
    {
        Compile("from OrderCreated key orderNumber\n  total = total");
        var safe = _slice.Projections.Single();
        Compile(Composite);
        _slice = _slice with { Projections = [safe, _slice.Projections.Single() with { Name = "Unrendered" }] };
        Render().Content.ShouldContain("[FromEvent<OrderCreated>(key: nameof(OrderCreated.OrderNumber))]");
    }

    [Fact]
    public void should_reject_the_rendered_first_projection_even_when_the_second_is_safe()
    {
        Compile("from OrderCreated key orderNumber\n  total = total");
        var safe = _slice.Projections.Single();
        Compile(Composite);
        _slice = _slice with { Projections = [_slice.Projections.Single(), safe with { Name = "Unrendered" }] };
        Assert.IsType<UnsupportedCompositeProjectionKey>(Catch.Exception(() => Render()));
    }

    [Fact]
    public void should_preserve_authored_names_and_the_exact_unnormalized_key_location()
    {
        Compile(Composite);
        var projection = _slice.Projections.Single();
        var from = projection.Blocks.OfType<FromSyntax>().Single();
        var location = SourceLocation.Start with { Path = "./Models/../Orders.play", Line = 37, Column = 13 };
        var composite = (CompositeKeySyntax)from.Key! with { Location = location };
        _slice = _slice with { Projections = [projection with { Name = "orderProjection", ReadModel = "orderView", Blocks = [@from with { Key = composite }] }] };
        var failure = Assert.IsType<UnsupportedCompositeProjectionKey>(Catch.Exception(() => Render()));
        failure.ProjectionName.ShouldEqual("orderProjection");
        failure.ReadModel.ShouldEqual("orderView");
        failure.SlicePath.ShouldEqual("Sales.Orders.Summary");
        failure.CompositeType.ShouldEqual("OrderKey");
        failure.Location.ShouldEqual(location);
    }

    [Fact]
    public void should_report_the_projection_name_when_the_read_model_name_is_omitted()
    {
        Compile(Composite);
        _slice = _slice with { Projections = [_slice.Projections.Single() with { ReadModel = null }] };
        Assert.IsType<UnsupportedCompositeProjectionKey>(Catch.Exception(() => Render())).ReadModel.ShouldEqual("Order");
    }

    [Fact]
    public void should_admit_queries_before_inspecting_root_keys()
    {
        Compile(Composite);
        var compilation = new NativeScreenplayCompiler().Compile("""
            module Sales
              feature Orders
                slice StateView Summary
                  query Orders => OrderReadModel[]
                    filter number String
            """);
        compilation.Success.ShouldBeTrue();
        _slice = _slice with { Queries = compilation.Value!.Modules.Single().Features.Single().Slices.Single().Queries };
        Assert.IsType<UnsupportedQueryIntent>(Catch.Exception(() => Render()));
    }

    [Theory]
    [InlineData("nested details\n  from OrderCreated\n    key OrderKey\n      customerId = customerId\n      orderNumber = orderNumber\n    total = total")]
    [InlineData("children lines identified by orderNumber\n  from OrderCreated\n    key OrderKey\n      customerId = customerId\n      orderNumber = orderNumber\n    orderNumber = orderNumber")]
    public void should_leave_non_root_composite_warning_behavior_unchanged(string blocks)
    {
        Compile(blocks);
        Render().Diagnostics.ShouldContain(diagnostic => diagnostic.Contains("event routes on the event source id", StringComparison.Ordinal));
    }

    [Fact]
    public void should_admit_a_supported_root_string_key_instead_of_rejecting_it_as_composite()
    {
        Compile("from OrderCreated\n  key literal \"global\"\n  total = total");
        Render().Content.ShouldContain("[FromEvent<OrderCreated>(ConstantKey = \"global\")]");
    }

    void Compile(string blocks)
    {
        const string template = """
            type OrderKey
              customerId String
              orderNumber String
            module Sales
              feature Orders
                slice StateView Summary
                  event OrderCreated
                    customerId String
                    orderNumber String
                    total Decimal
                  event OrderUpdated
                    customerId String
                    orderNumber String
                    total Decimal
                  projection Order => OrderReadModel
                    BLOCKS
            """;
        var source = template.Replace("BLOCKS", blocks.Replace("\n", "\n        ", StringComparison.Ordinal), StringComparison.Ordinal);
        var compilation = new NativeScreenplayCompiler().Compile(source);
        Assert.True(compilation.Success, $"{string.Join(Environment.NewLine, compilation.Diagnostics)}{Environment.NewLine}{source}");
        compilation.Diagnostics.ShouldBeEmpty();
        _context = new([compilation.Value!]);
        _slice = _context.Slices.Single().Slice;
    }

    RenderedFile Render() => new StateViewSliceRenderer().Render(new LocatedSlice(_slice, ["Sales", "Orders"]), _context, "OrdersApp");
}
