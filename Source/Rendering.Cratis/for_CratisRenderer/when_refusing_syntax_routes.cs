// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Specifications;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_CratisRenderer.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisRenderer;

public class when_refusing_syntax_routes : a_multi_slice_application
{
    [Theory]
    [InlineData("source")]
    [InlineData("command")]
    [InlineData("example-stream")]
    [InlineData("example-no-stream")]
    [InlineData("redelivery-stream")]
    [InlineData("redelivery-no-stream")]
    [InlineData("occurrence-stream")]
    [InlineData("occurrence-no-stream")]
    public async Task should_refuse_every_public_entrypoint_before_any_output(string form)
    {
        var location = SourceLocation.Start with { Path = "Routes.play", Line = 10 };
        var module = _application.Modules.Single();
        var feature = module.Features.Single();
        var selected = WithRoute(feature.Slices.First(), form, location);
        feature = feature with { Slices = [selected] };
        module = module with { Features = [feature] };
        _application = _application with
        {
            Modules = [module],
            EventSources = form == "source" ? [new EventSourceSyntax("Invoice", location) { Streams = [new("Changes", location)] }] : []
        };
        var context = new ApplicationSet([_application]);
        foreach (var scope in new[] { "application", "module", "feature", "slice" })
        {
            var error = await Catch.Exception(() => scope switch
            {
                "application" => _renderer.Render([_application], _targetDirectory, _output, _error),
                "module" => _renderer.Render(module, context, _targetDirectory, _output, _error),
                "feature" => _renderer.Render(feature, context, _targetDirectory, _output, _error),
                _ => _renderer.Render(selected, context, _targetDirectory, _output, _error)
            });
            error.ShouldBeOfExactType<RenderingFailed>();
            var failure = ((RenderingFailed)error).Failures.Single();
            failure.ShouldBeOfExactType<UnsupportedEventRoutes>();
            ((UnsupportedEventRoutes)failure).Location.ShouldEqual(form == "command" ? selected.Commands.Single().Location : location);
            failure.Message.ShouldContain("STAGE-ESM-016:");
            _scaffolder.WasCalled.ShouldBeFalse();
            _codeOutput.Files.ShouldBeEmpty();
            _codeOutput.FailureMarkerWasWritten.ShouldBeFalse();
        }
    }

    [Theory]
    [InlineData("command")]
    [InlineData("example-stream")]
    [InlineData("example-no-stream")]
    [InlineData("redelivery-stream")]
    [InlineData("redelivery-no-stream")]
    [InlineData("occurrence-stream")]
    [InlineData("occurrence-no-stream")]
    public async Task should_ignore_an_unsafe_unselected_slice_in_the_application_set(string form)
    {
        var module = _application.Modules.Single();
        var feature = module.Features.Single();
        var selected = feature.Slices.First();
        var sibling = feature.Slices.Last() with { Commands = [selected.Commands.Single() with { Name = "RouteInvoice" }] };
        sibling = WithRoute(sibling, form, SourceLocation.Start);
        _application = _application with { Modules = [module with { Features = [feature with { Slices = [selected, sibling] }] }] };
        var context = new ApplicationSet([_application]);
        var selectedFeature = feature with { Slices = [selected] };
        var selectedModule = module with { Features = [selectedFeature] };
        foreach (var scope in new[] { "module", "feature", "slice" })
        {
            await (scope switch
            {
                "module" => _renderer.Render(selectedModule, context, _targetDirectory, _output, _error),
                "feature" => _renderer.Render(selectedFeature, context, _targetDirectory, _output, _error, module: "Billing"),
                _ => _renderer.Render(selected, context, _targetDirectory, _output, _error, module: "Billing", feature: "Invoices")
            });
            _codeOutput.Files.ShouldContain(file => file.Content.Contains("RegisterInvoice", StringComparison.Ordinal));
            _codeOutput.Files.ShouldNotContain(file => file.RelativePath.Contains("InvoiceSummary", StringComparison.Ordinal));
            _codeOutput.FailureMarkerWasWritten.ShouldBeFalse();
            _error.ToString().ShouldBeEmpty();
        }
    }

    static SliceSyntax WithRoute(SliceSyntax slice, string form, SourceLocation location)
    {
        var route = new SpecificationStreamSyntax("Invoice", "Changes", location);
        var noStream = new SpecificationNoStreamSyntax(location);
        var example = new SpecificationExampleSyntax("Registered", "InvoiceRegistered", [], location)
        {
            Stream = form == "example-stream" ? route : null,
            NoStream = form == "example-no-stream" ? noStream : null
        };
        var redelivery = new SpecificationRedeliverySyntax("InvoiceRegistered", "React", [], location)
        {
            Stream = form == "redelivery-stream" ? route : null,
            NoStream = form == "redelivery-no-stream" ? noStream : null
        };
        var occurrence = new SpecificationEventSyntax("InvoiceRegistered", [], location)
        {
            Stream = form == "occurrence-stream" ? route : null,
            NoStream = form == "occurrence-no-stream" ? noStream : null
        };
        var specification = new SpecificationSyntax("Routes", [], null, [], [], location)
        {
            WhenRedelivered = form.StartsWith("redelivery", StringComparison.Ordinal) ? redelivery : null,
            ThenEvents = form.StartsWith("occurrence", StringComparison.Ordinal) ? [occurrence] : []
        };

        return slice with
        {
            Commands = form == "command" ? [slice.Commands.Single() with { Stream = new("Invoice", "Changes", location) }] : slice.Commands,
            Specifications = [specification],
            Examples = form.StartsWith("example", StringComparison.Ordinal) ? [example] : []
        };
    }
}
