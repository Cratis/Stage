// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax.Projections;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisRenderer.given;
using Cratis.Stage.Rendering.Cratis.Renderers;
using Cratis.Stage.Rendering.Cratis.Specifications;
using Xunit;

using NativeScreenplayCompiler = Cratis.Screenplay.ScreenplayCompiler;

namespace Cratis.Stage.Rendering.Cratis.for_CratisRenderer;

public class when_admitting_propertyless_root_string_keys : a_multi_slice_application
{
    [Theory]
    [InlineData("global", UnsupportedRootStringKeyReason.PropertylessRecord)]
    [InlineData("", UnsupportedRootStringKeyReason.EmptyLiteral)]
    public async Task should_reject_the_active_profile_without_source_or_specifications(string value, UnsupportedRootStringKeyReason reason)
    {
        Compile($"key literal \"{value}\"");
        var slice = _application.Modules.Single().Features.Single().Slices.Single();
        var projection = slice.Projections.Single();
        SpecificationRenderer.Unrenderable(slice.Specifications.Single(), slice).ShouldBeNull();
        IRenderer renderer = _renderer;

        var exception = await Catch.Exception(() => renderer.Render([_application], _targetDirectory, _output, _error));

        Assert.True(exception is RenderingFailed, $"Expected typed rejection, but emitted:{Environment.NewLine}{string.Join(Environment.NewLine, _codeOutput.Files.Select(file => file.Content))}");
        var failure = Assert.IsType<RenderingFailed>(exception);
        var rejection = Assert.IsType<UnsupportedRootStringKey>(Assert.Single(failure.Failures));
        rejection.Reason.ShouldEqual(reason);
        rejection.SlicePath.ShouldEqual("Sales.Orders.Summary");
        rejection.ProjectionName.ShouldEqual("Order");
        rejection.ReadModel.ShouldEqual("OrderReadModel");
        rejection.EventName.ShouldEqual("OrderCreated");
        var key = (ExpressionKeySyntax)projection.Blocks.OfType<FromSyntax>().Single().Key!;
        rejection.Location.ShouldEqual(reason == UnsupportedRootStringKeyReason.PropertylessRecord ? projection.Location : key.Expression.Location);
        rejection.Message.ShouldContain("STAGE-CRATIS-KEY-002");
        _error.ToString().ShouldContain(rejection.Message);
        _output.ToString().ShouldNotContain("Rendering complete.");
        _codeOutput.FailureMarkerWasWritten.ShouldBeTrue();
        _codeOutput.Files.ShouldBeEmpty();
    }

    [Fact]
    public async Task should_keep_the_inactive_propertyless_default_profile_unchanged()
    {
        Compile(string.Empty);
        IRenderer renderer = _renderer;

        await renderer.Render([_application], _targetDirectory, _output, _error);

        var file = Assert.Single(_codeOutput.Files, file => file.RelativePath.EndsWith("Summary.cs", StringComparison.Ordinal));
        var source = file.Content;
        source.ShouldContain("public record OrderReadModel()");
        source.ShouldContain("[FromEvent<OrderCreated>]");
        source.ShouldContain("OrderById(IReadModels readModels, Guid id)");
        source.ShouldNotContain("ConstantKey");

        // StateView does not emit commands; compile its source, not the separate command specification.
        var model = RenderedOutput.Load([file]).GetTypes().Single(type => type.Name == "OrderReadModel");
        model.GetProperties().ShouldBeEmpty();
        model.GetConstructors().Single().GetParameters().ShouldBeEmpty();
        RenderedOutput.Errors([file]).ShouldBeEmpty();
        RenderedOutput.Warnings([file]).ShouldBeEmpty();
        _output.ToString().ShouldContain("Rendering complete.");
        _codeOutput.FailureMarkerWasWritten.ShouldBeFalse();
    }

    void Compile(string key)
    {
        var compilation = new NativeScreenplayCompiler().Compile($"""
            module Sales
              feature Orders
                slice StateView Summary
                  event OrderCreated
                    total Decimal
                  command CreateOrder
                    produces OrderCreated
                  projection Order => OrderReadModel
                    from OrderCreated
                      {key}
                  query OrderById => OrderReadModel
                  specification Creation
                    when CreateOrder
                    then OrderCreated
            """);
        Assert.True(compilation.Success, string.Join(Environment.NewLine, compilation.Diagnostics));
        compilation.Diagnostics.ShouldBeEmpty();
        _application = compilation.Value!;
    }
}
