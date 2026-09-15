// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Projections;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisRenderer.given;
using Cratis.Stage.Rendering.Cratis.Renderers;
using Cratis.Stage.Rendering.Cratis.Specifications;
using Xunit;

using NativeScreenplayCompiler = Cratis.Screenplay.ScreenplayCompiler;

namespace Cratis.Stage.Rendering.Cratis.for_CratisRenderer;

public class when_admitting_selected_composite_projection_keys : a_multi_slice_application
{
    ModuleSyntax _module = null!;
    FeatureSyntax _feature = null!;
    SliceSyntax _blocked = null!;
    SliceSyntax _independent = null!;
    ApplicationSet _context = null!;

    void Establish()
    {
        const string source = """
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
                  command CreateOrder
                    produces OrderCreated
                  projection Order => OrderReadModel
                    from OrderCreated
                      key OrderKey
                        customerId = customerId
                        orderNumber = orderNumber
                      total = total
                  specification Creation
                    when CreateOrder
                    then OrderCreated
                slice StateView Independent
                  event OrderArchived
                    orderNumber String
                  projection Archive => ArchivedOrder
                    from OrderArchived
                      key orderNumber
                      orderNumber = orderNumber
            """;
        var compilation = new NativeScreenplayCompiler().Compile(source);
        Assert.True(compilation.Success, string.Join(Environment.NewLine, compilation.Diagnostics));
        compilation.Diagnostics.ShouldBeEmpty();
        _application = compilation.Value!;
        _module = _application.Modules.Single();
        _feature = _module.Features.Single();
        _blocked = _feature.Slices.First();
        _independent = _feature.Slices.Last();
        _context = new([_application]);
    }

    [Theory]
    [InlineData("application")]
    [InlineData("module")]
    [InlineData("feature")]
    [InlineData("slice")]
    public async Task should_fail_closed_through_each_public_scope_and_continue_independent_output(string scope)
    {
        // This spec would be admitted by the existing specification path, so its absence tests early return.
        SpecificationRenderer.Unrenderable(_blocked.Specifications.Single(), _blocked).ShouldBeNull();
        IRenderer renderer = _renderer;
        var exception = await Catch.Exception(() => scope switch
        {
            "application" => renderer.Render([_application], _targetDirectory, _output, _error),
            "module" => _renderer.Render(_module, _context, _targetDirectory, _output, _error),
            "feature" => _renderer.Render(_feature, _context, _targetDirectory, _output, _error, module: "Sales"),
            _ => _renderer.Render(_blocked, _context, _targetDirectory, _output, _error, module: "Sales", feature: "Orders")
        });
        var failure = Assert.IsType<RenderingFailed>(exception);
        var rejection = Assert.IsType<UnsupportedCompositeProjectionKey>(Assert.Single(failure.Failures));
        rejection.SlicePath.ShouldEqual("Sales.Orders.Summary");
        rejection.ProjectionName.ShouldEqual("Order");
        rejection.ReadModel.ShouldEqual("OrderReadModel");
        rejection.EventName.ShouldEqual("OrderCreated");
        rejection.CompositeType.ShouldEqual("OrderKey");
        rejection.Location.ShouldEqual(_blocked.Projections.Single().Blocks.OfType<FromSyntax>().Single().Key!.Location);
        _error.ToString().ShouldContain(rejection.Message);
        _output.ToString().ShouldNotContain("Rendering complete.");
        _codeOutput.FailureMarkerWasWritten.ShouldBeTrue();
        _codeOutput.Files.ShouldNotContain(file => file.RelativePath.Contains("Summary", StringComparison.Ordinal));
        if (scope == "slice")
        {
            _codeOutput.Files.ShouldBeEmpty();
        }
        else
        {
            _codeOutput.Files.ShouldContain(file => file.RelativePath == Path.Combine("Sales", "Orders", "Independent", "Independent.cs"));
            RenderedOutput.Errors(_codeOutput.Files).ShouldBeEmpty();
        }

        if (scope == "application")
        {
            _codeOutput.Files.ShouldContain(file => file.RelativePath == Path.Combine("Common", "OrderKey.cs"));
        }
    }

    [Theory]
    [InlineData("module")]
    [InlineData("feature")]
    [InlineData("slice")]
    public async Task should_ignore_the_unsafe_unselected_slice_in_the_resolution_context(string scope)
    {
        var feature = _feature with { Slices = [_independent] };
        var module = _module with { Features = [feature] };
        await (scope switch
        {
            "module" => _renderer.Render(module, _context, _targetDirectory, _output, _error),
            "feature" => _renderer.Render(feature, _context, _targetDirectory, _output, _error, module: "Sales"),
            _ => _renderer.Render(_independent, _context, _targetDirectory, _output, _error, module: "Sales", feature: "Orders")
        });
        var file = Assert.Single(_codeOutput.Files);
        file.Content.ShouldContain("[FromEvent<OrderArchived>(key: nameof(OrderArchived.OrderNumber))]");
        RenderedOutput.Errors(_codeOutput.Files).ShouldBeEmpty();
        _error.ToString().ShouldBeEmpty();
        _output.ToString().ShouldContain("Rendering complete.");
        _codeOutput.FailureMarkerWasWritten.ShouldBeFalse();
    }
}
