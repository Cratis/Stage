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

public class when_admitting_selected_root_string_keys : a_multi_slice_application
{
    ModuleSyntax _module = null!;
    FeatureSyntax _feature = null!;
    SliceSyntax _blocked = null!;
    SliceSyntax _independent = null!;
    ApplicationSet _context = null!;

    void Establish()
    {
        var compilation = new NativeScreenplayCompiler().Compile("""
            module Sales
              feature Orders
                slice StateView Summary
                  event OrderCreated
                    total Decimal
                  command CreateOrder
                    produces OrderCreated
                  projection Order => OrderReadModel
                    from OrderCreated
                      key literal ""
                      total = total
                  specification Creation
                    when CreateOrder
                    then OrderCreated
                slice StateView Independent
                  event OrderArchived
                    number String
                  projection Archive => ArchivedOrder
                    from OrderArchived
                      number = number
            """);
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
    public async Task should_reject_selected_unsafe_literals_without_returning_affected_artifacts(string scope)
    {
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
        var rejection = Assert.IsType<UnsupportedRootStringKey>(Assert.Single(failure.Failures));
        rejection.Reason.ShouldEqual(UnsupportedRootStringKeyReason.EmptyLiteral);
        rejection.SlicePath.ShouldEqual("Sales.Orders.Summary");
        rejection.ProjectionName.ShouldEqual("Order");
        rejection.ReadModel.ShouldEqual("OrderReadModel");
        rejection.EventName.ShouldEqual("OrderCreated");
        var key = (ExpressionKeySyntax)_blocked.Projections.Single().Blocks.OfType<FromSyntax>().Single().Key!;
        rejection.Location.ShouldEqual(key.Expression.Location);
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
            Assert.Single(_codeOutput.Files).RelativePath.ShouldEqual(Path.Combine("Sales", "Orders", "Independent", "Independent.cs"));
            RenderedOutput.Errors(_codeOutput.Files).ShouldBeEmpty();
        }
    }

    [Theory]
    [InlineData("module")]
    [InlineData("feature")]
    [InlineData("slice")]
    public async Task should_ignore_unsafe_literals_in_unselected_slices(string scope)
    {
        var feature = _feature with { Slices = [_independent] };
        var module = _module with { Features = [feature] };
        await (scope switch
        {
            "module" => _renderer.Render(module, _context, _targetDirectory, _output, _error),
            "feature" => _renderer.Render(feature, _context, _targetDirectory, _output, _error, module: "Sales"),
            _ => _renderer.Render(_independent, _context, _targetDirectory, _output, _error, module: "Sales", feature: "Orders")
        });
        Assert.Single(_codeOutput.Files).Content.ShouldContain("[FromEvent<OrderArchived>]");
        _error.ToString().ShouldBeEmpty();
        _output.ToString().ShouldContain("Rendering complete.");
        _codeOutput.FailureMarkerWasWritten.ShouldBeFalse();
    }
}
