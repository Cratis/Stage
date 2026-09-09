// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax.Projections;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisRenderer.given;
using Cratis.Stage.Rendering.Cratis.Renderers;
using Xunit;

using NativeScreenplayCompiler = Cratis.Screenplay.ScreenplayCompiler;

namespace Cratis.Stage.Rendering.Cratis.for_CratisRenderer;

public class when_rendering_authored_composite_projection_keys : a_multi_slice_application
{
    // The FROM block is the released Screenplay 4.10 compiler fixture (2af8ebd), embedded in an
    // application with its event and required complex schema, not compiled as a standalone projection.
    const string Source = """
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
              readmodel OrderReadModel
                total Decimal
              projection Order => OrderReadModel
                from OrderCreated
                  key OrderKey
                    customerId = customerId
                    orderNumber = orderNumber
                  total = total
              specification Creation
                when CreateOrder
                then OrderCreated
        """;

    [Fact]
    public async Task should_reject_the_native_root_composite_instead_of_writing_default_identity()
    {
        var compilation = new NativeScreenplayCompiler().Compile(Source);
        Assert.True(compilation.Success, $"Invalid authored application: {string.Join(Environment.NewLine, compilation.Diagnostics)}");
        compilation.Diagnostics.ShouldBeEmpty();
        _application = compilation.Value!;
        var projection = _application.Modules.Single().Features.Single().Slices.Single().Projections.Single();
        var from = Assert.IsType<FromSyntax>(Assert.Single(projection.Blocks));
        var key = Assert.IsType<CompositeKeySyntax>(from.Key);
        key.Type.ShouldEqual("OrderKey");
        key.Parts.Select(part => part.Property).ShouldContainOnly("customerId", "orderNumber");
        from.Mappings.OfType<SetMappingSyntax>().Single().Property.ShouldEqual("total");
        key.Location.Path.ShouldBeNull();
        key.Location.ShouldEqual(SourceLocation.Start with { Line = 15, Column = 11 });
        Source.Split('\n')[key.Location.Line - 1].Trim().ShouldEqual("key OrderKey");

        IRenderer renderer = _renderer;
        var exception = await Catch.Exception(() => renderer.Render([_application], _targetDirectory, _output, _error));
        var evidence = $"Native compiler: {typeof(NativeScreenplayCompiler).Assembly.FullName}{Environment.NewLine}" +
            $"Route: IRenderer.Render(applications) -> LegacyRendererCompatibilityAdapter -> StateViewSliceRenderer{Environment.NewLine}" +
            $"Authored key: {key.Type} at {key.Location}{Environment.NewLine}" +
            $"Actual exception: {exception?.ToString() ?? "<none>"}; failure marker: {_codeOutput.FailureMarkerWasWritten}{Environment.NewLine}" +
            $"Output:{Environment.NewLine}{_output}{Environment.NewLine}Diagnostics:{Environment.NewLine}{_error}{Environment.NewLine}" +
            string.Join(Environment.NewLine, _codeOutput.Files.Select(file => $"Artifact: {file.RelativePath}{Environment.NewLine}{file.Content}"));
        Assert.True(
            exception is RenderingFailed &&
            !_codeOutput.Files.Any(file => file.RelativePath.Contains("Summary", StringComparison.Ordinal)) &&
            !_output.ToString().Contains("Rendering complete.", StringComparison.Ordinal),
            $"An explicit composite identity must not become the event-source-id default. Expected RenderingFailed and no affected read model or specification.{Environment.NewLine}{evidence}");
        var failure = Assert.IsType<RenderingFailed>(exception);
        var rejection = Assert.IsType<UnsupportedCompositeProjectionKey>(Assert.Single(failure.Failures));
        UnsupportedCompositeProjectionKey.DiagnosticCode.ShouldEqual("STAGE-CRATIS-KEY-001");
        rejection.SlicePath.ShouldEqual("Sales.Orders.Summary");
        rejection.ProjectionName.ShouldEqual("Order");
        rejection.ReadModel.ShouldEqual("OrderReadModel");
        rejection.EventName.ShouldEqual("OrderCreated");
        rejection.CompositeType.ShouldEqual("OrderKey");
        rejection.Location.ShouldEqual(key.Location);
        rejection.Message.ShouldContain(UnsupportedCompositeProjectionKey.DiagnosticCode);
        rejection.Message.ShouldContain("event source id");
        _error.ToString().ShouldContain(rejection.Message);
        _error.ToString().ShouldNotContain("Specification 'Creation'");
        _codeOutput.FailureMarkerWasWritten.ShouldBeTrue();

        // Common type emission is independent; this is not application-wide atomicity.
        Assert.Single(_codeOutput.Files).RelativePath.ShouldEqual(Path.Combine("Common", "OrderKey.cs"));
        RenderedOutput.Errors(_codeOutput.Files).ShouldBeEmpty();
    }
}
