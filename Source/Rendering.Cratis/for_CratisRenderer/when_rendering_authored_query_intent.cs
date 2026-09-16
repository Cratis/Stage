// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisRenderer.given;
using Cratis.Stage.Rendering.Cratis.Renderers;
using Xunit;

using NativeScreenplayCompiler = Cratis.Screenplay.ScreenplayCompiler;

namespace Cratis.Stage.Rendering.Cratis.for_CratisRenderer;

public class when_rendering_authored_query_intent : a_multi_slice_application
{
    [Fact]
    public async Task should_reject_a_declared_filter_parameter_without_writing_its_read_model()
    {
        const string source = """
            policy Readers
              require authenticated
            module Billing
              feature Invoices
                slice StateView Summary
                  event InvoiceRegistered
                    status String
                  readmodel InvoiceSummary
                    status String
                  projection InvoiceSummaryProjection => InvoiceSummary
                    from InvoiceRegistered
                      status = status
                  query InvoicesWithStatus => InvoiceSummary[]
                    authorize Readers
                    filter status String
            """;
        var query = Compile(source, "InvoicesWithStatus");
        var filter = Assert.Single(query.Filters);
        filter.Name.ShouldEqual("status");
        filter.Type.Name.ShouldEqual("String");
        filter.Source.ShouldBeNull();
        query.Performer.ShouldBeNull();
        AssertLocation(source, filter.Location, "filter status String", 9);

        // Screenplay 4.10 declares a parameter here, not an executable equality predicate.
        await RejectSelected(query, UnsupportedQueryIntentReason.FilterContract, filter.Location);
    }

    [Fact]
    public async Task should_reject_a_symbolic_file_performer_without_writing_its_read_model()
    {
        const string source = """
            policy Readers
              require authenticated
            module Billing
              feature Invoices
                slice StateView Summary
                  event InvoiceRegistered
                    status String
                  readmodel InvoiceSummary
                    status String
                  projection InvoiceSummaryProjection => InvoiceSummary
                    from InvoiceRegistered
                      status = status
                  query CuratedInvoices => InvoiceSummary[]
                    authorize Readers
                    performer
                      file QueryPerformers/CuratedInvoices.cs
            """;
        var query = Compile(source, "CuratedInvoices");
        query.Filters.ShouldBeEmpty();
        query.Performer.ShouldNotBeNull();
        query.Performer!.Code.ShouldBeNull();
        query.Performer.File!.Path.ShouldEqual("QueryPerformers/CuratedInvoices.cs");
        AssertLocation(source, query.Performer.Location, "performer", 9);
        AssertLocation(source, query.Performer.File.Location, "file QueryPerformers/CuratedInvoices.cs", 11);

        // The file reference is a valid symbolic attachment. No file is created, opened or resolved.
        await RejectSelected(query, UnsupportedQueryIntentReason.FilePerformer, query.Performer.File.Location);
    }

    [Fact]
    public async Task should_reject_an_inline_performer_without_writing_its_read_model()
    {
        const string source = """
            policy Readers
              require authenticated
            module Billing
              feature Invoices
                slice StateView Summary
                  event InvoiceRegistered
                    status String
                  readmodel InvoiceSummary
                    status String
                  projection InvoiceSummaryProjection => InvoiceSummary
                    from InvoiceRegistered
                      status = status
                  query EmptyInvoices => InvoiceSummary[]
                    authorize Readers
                    performer
                      csharp
                        ```
                        return Array.Empty<InvoiceSummary>().AsQueryable();
                        ```
            """;
        var query = Compile(source, "EmptyInvoices");
        query.Filters.ShouldBeEmpty();
        query.Performer.ShouldNotBeNull();
        query.Performer!.File.ShouldBeNull();
        query.Performer.Code!.Language.ShouldEqual("csharp");
        query.Performer.Code.Code.ShouldEqual("return Array.Empty<InvoiceSummary>().AsQueryable();");
        AssertLocation(source, query.Performer.Location, "performer", 9);
        AssertLocation(source, query.Performer.Code.Location, "csharp", 11);

        await RejectSelected(query, UnsupportedQueryIntentReason.InlinePerformer, query.Performer.Code.Location);
    }

    static void AssertLocation(string source, SourceLocation location, string declaration, int column)
    {
        // Compile(string) has no document path parameter: preserve its native null Path, never patch the AST.
        location.Path.ShouldBeNull();
        location.Column.ShouldEqual(column);
        source.Split('\n')[location.Line - 1].Trim().ShouldEqual(declaration);
    }

    QuerySyntax Compile(string source, string queryName)
    {
        var compilation = new NativeScreenplayCompiler().Compile(source);
        Assert.True(compilation.Success, $"Invalid authored fixture:{Environment.NewLine}{string.Join(Environment.NewLine, compilation.Diagnostics)}{Environment.NewLine}{source}");
        compilation.Diagnostics.ShouldBeEmpty();
        _application = compilation.Value!;
        _targetDirectory = new DirectoryInfo(Path.Combine(Path.GetTempPath(), "app-query-intent-admission"));
        var slice = _application.Modules.Single().Features.Single().Slices.Single();
        slice.Type.ShouldEqual(SliceType.StateView);
        var query = Assert.Single(slice.Queries);
        query.Name.ShouldEqual(queryName);
        query.ReturnType.Name.ShouldEqual("InvoiceSummary");
        query.ReturnType.IsCollection.ShouldBeTrue();
        query.By.ShouldBeNull();
        query.IsObservable.ShouldBeFalse();
        query.Authorize.ShouldNotBeNull();
        slice.Projections.Single().ReadModel.ShouldEqual(query.ReturnType.Name);
        slice.ReadModels!.Single().Name.ShouldEqual(query.ReturnType.Name);
        AssertLocation(source, query.Location, $"query {queryName} => InvoiceSummary[]", 7);
        return query;
    }

    async Task RejectSelected(QuerySyntax query, UnsupportedQueryIntentReason reason, SourceLocation location)
    {
        QueryRenderer.IsFullyRendered(query).ShouldBeFalse();
        IRenderer renderer = _renderer;
        var exception = await Catch.Exception(() => renderer.Render([_application], _targetDirectory, _output, _error));
        var files = _codeOutput.Files;
        var evidence = $"Native Screenplay assembly: {typeof(NativeScreenplayCompiler).Assembly.FullName}{Environment.NewLine}" +
            $"Route: IRenderer.Render(applications) -> LegacyRendererCompatibilityAdapter -> StateViewSliceRenderer -> QueryRenderer{Environment.NewLine}" +
            $"Slice: Billing.Invoices.Summary; query: {query.Name}; location: {query.Location}{Environment.NewLine}" +
            $"Filters: [{string.Join(", ", query.Filters.Select(filter => $"{filter.Name} {filter.Type.Name} at {filter.Location}; Source={filter.Source}"))}]{Environment.NewLine}" +
            $"Performer: {query.Performer}{Environment.NewLine}" +
            $"Expected rejection: {reason} at {location}{Environment.NewLine}" +
            $"Actual exception: {exception?.ToString() ?? "<none>"}; failure marker: {_codeOutput.FailureMarkerWasWritten}{Environment.NewLine}" +
            $"Output:{Environment.NewLine}{_output}{Environment.NewLine}Diagnostics:{Environment.NewLine}{_error}{Environment.NewLine}" +
            string.Join(Environment.NewLine, files.Select(file => $"Artifact: {file.RelativePath}{Environment.NewLine}{file.Content}"));

        Assert.True(
            exception is RenderingFailed &&
            !files.Any(file => file.RelativePath == Path.Combine("Billing", "Invoices", "Summary", "Summary.cs")) &&
            !_output.ToString().Contains("Rendering complete.", StringComparison.Ordinal),
            $"Unsupported explicit query intent must fail with RenderingFailed and write no affected artifact.{Environment.NewLine}{evidence}");
        var failure = Assert.IsType<RenderingFailed>(exception);
        var intent = Assert.IsType<UnsupportedQueryIntent>(Assert.Single(failure.Failures));
        UnsupportedQueryIntent.DiagnosticCode.ShouldEqual("STAGE-CRATIS-QUERY-001");
        intent.QueryName.ShouldEqual(query.Name);
        intent.ReadModel.ShouldEqual(query.ReturnType.Name);
        intent.Location.ShouldEqual(location);
        intent.Reason.ShouldEqual(reason);
        intent.SlicePath.ShouldEqual("Billing.Invoices.Summary");
        intent.Message.ShouldContain(UnsupportedQueryIntent.DiagnosticCode);
        intent.Message.ShouldNotContain("return Array.Empty");
        _error.ToString().ShouldContain(UnsupportedQueryIntent.DiagnosticCode);
        files.ShouldBeEmpty();
        _codeOutput.FailureMarkerWasWritten.ShouldBeTrue();
    }
}
