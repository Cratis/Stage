// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_CratisRenderer.given;
using Cratis.Stage.Rendering.Cratis.Renderers;
using Xunit;

using NativeScreenplayCompiler = Cratis.Screenplay.ScreenplayCompiler;

namespace Cratis.Stage.Rendering.Cratis.for_CratisRenderer;

public class when_admitting_selected_query_intent : a_multi_slice_application
{
    ModuleSyntax _module = null!;
    FeatureSyntax _feature = null!;
    SliceSyntax _blocked = null!;
    SliceSyntax _independent = null!;
    ApplicationSet _context = null!;

    void Establish()
    {
        const string source = """
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
                  query ListedInvoices => InvoiceSummary[]
                  query CuratedInvoices => InvoiceSummary[]
                    performer
                      file QueryPerformers/CuratedInvoices.cs
                  specification Registration
                    when RegisterInvoice
                    then InvoiceRegistered
                slice StateView Independent
                  event InvoiceArchived
                    status String
                  readmodel ArchivedInvoice
                    status String
                  projection ArchivedInvoiceProjection => ArchivedInvoice
                    from InvoiceArchived
                      status = status
                  query ArchivedInvoices => ArchivedInvoice[]
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
    [InlineData("application", UnsupportedQueryIntentReason.FilterContract)]
    [InlineData("module", UnsupportedQueryIntentReason.FilterContract)]
    [InlineData("feature", UnsupportedQueryIntentReason.FilterContract)]
    [InlineData("slice", UnsupportedQueryIntentReason.FilterContract)]
    [InlineData("application", UnsupportedQueryIntentReason.FilePerformer)]
    [InlineData("module", UnsupportedQueryIntentReason.FilePerformer)]
    [InlineData("feature", UnsupportedQueryIntentReason.FilePerformer)]
    [InlineData("slice", UnsupportedQueryIntentReason.FilePerformer)]
    [InlineData("application", UnsupportedQueryIntentReason.InlinePerformer)]
    [InlineData("module", UnsupportedQueryIntentReason.InlinePerformer)]
    [InlineData("feature", UnsupportedQueryIntentReason.InlinePerformer)]
    [InlineData("slice", UnsupportedQueryIntentReason.InlinePerformer)]
    public async Task should_reject_a_later_unsafe_query_through_every_public_scope(string scope, UnsupportedQueryIntentReason reason)
    {
        var query = UnsafeQuery(reason);
        ReplaceBlocked(_blocked with { Queries = [_blocked.Queries.First(), query] });
        var exception = await Catch.Exception(() => Render(scope));
        AssertFailure(exception, query, reason, RelevantLocation(query));
        if (scope == "slice")
        {
            _codeOutput.Files.ShouldBeEmpty();
        }
        else
        {
            var file = Assert.Single(_codeOutput.Files);
            file.RelativePath.ShouldEqual(Path.Combine("Billing", "Invoices", "Independent", "Independent.cs"));
            file.Content.ShouldContain("IQueryable<ArchivedInvoice> ArchivedInvoices(");
            RenderedOutput.Errors(_codeOutput.Files).ShouldBeEmpty();
        }
    }

    [Fact]
    public async Task should_reject_an_unsafe_unmatched_return_model_in_the_selected_slice()
    {
        var query = _blocked.Queries.Last();
        query = query with { ReturnType = query.ReturnType with { Name = "UnmatchedModel" } };
        ReplaceBlocked(_blocked with { Queries = [_blocked.Queries.First(), query] });
        var exception = await Catch.Exception(() => Render("slice"));
        AssertFailure(exception, query, UnsupportedQueryIntentReason.FilePerformer, query.Performer!.File!.Location);
        _codeOutput.Files.ShouldBeEmpty();
    }

    [Fact]
    public void should_admit_queries_before_any_selected_slice_projection_is_inspected()
    {
        // With no projection to render, an ownership-only check would incorrectly skip this unsafe query.
        var query = _blocked.Queries.Last();
        var slice = _blocked with { Projections = [] };
        var exception = Catch.Exception(() => new StateViewSliceRenderer().Render(new LocatedSlice(slice, ["Billing", "Invoices"]), _context, "BillingApp"));
        var failure = Assert.IsType<UnsupportedQueryIntent>(exception);
        failure.QueryName.ShouldEqual(query.Name);
        failure.SlicePath.ShouldEqual("Billing.Invoices.Summary");
        failure.Location.ShouldEqual(query.Performer!.File!.Location);
    }

    [Theory]
    [InlineData("module")]
    [InlineData("feature")]
    [InlineData("slice")]
    public async Task should_ignore_an_unsafe_unselected_slice_in_the_application_set(string scope)
    {
        // Keep the complete unsafe application in the resolution context, but select only the valid member.
        var validFeature = _feature with { Slices = [_independent] };
        var validModule = _module with { Features = [validFeature] };
        await (scope switch
        {
            "module" => _renderer.Render(validModule, _context, _targetDirectory, _output, _error),
            "feature" => _renderer.Render(validFeature, _context, _targetDirectory, _output, _error, module: "Billing"),
            _ => _renderer.Render(_independent, _context, _targetDirectory, _output, _error, module: "Billing", feature: "Invoices")
        });
        var file = Assert.Single(_codeOutput.Files);
        file.Content.ShouldContain("IQueryable<ArchivedInvoice> ArchivedInvoices(");
        _error.ToString().ShouldBeEmpty();
        _output.ToString().ShouldContain("Rendering complete.");
        _codeOutput.FailureMarkerWasWritten.ShouldBeFalse();
        RenderedOutput.Errors(_codeOutput.Files).ShouldBeEmpty();
    }

    QuerySyntax UnsafeQuery(UnsupportedQueryIntentReason reason)
    {
        var existing = _blocked.Queries.Last();
        if (reason == UnsupportedQueryIntentReason.FilePerformer)
        {
            return existing;
        }

        if (reason == UnsupportedQueryIntentReason.InlinePerformer)
        {
            return existing with { Performer = existing.Performer! with { File = null, Code = new CodeBlockSyntax("csharp", "return Array.Empty<InvoiceSummary>().AsQueryable();", SourceLocation.Start with { Path = "./Models/../Invoices.play", Line = 19, Column = 11 }) } };
        }

        const string source = """
            module Billing
              feature Invoices
                slice StateView Filtered
                  query FilteredInvoices => InvoiceSummary[]
                    filter status String
            """;
        var compilation = new NativeScreenplayCompiler().Compile(source);
        compilation.Success.ShouldBeTrue();
        compilation.Diagnostics.ShouldBeEmpty();
        return compilation.Value!.Modules.Single().Features.Single().Slices.Single().Queries.Single();
    }

    static SourceLocation RelevantLocation(QuerySyntax query) =>
        query.Filters.FirstOrDefault()?.Location ?? query.Performer!.File?.Location ?? query.Performer!.Code!.Location;

    void ReplaceBlocked(SliceSyntax slice)
    {
        _blocked = slice;
        _feature = _feature with { Slices = [_blocked, _independent] };
        _module = _module with { Features = [_feature] };
        _application = _application with { Modules = [_module] };
        _context = new([_application]);
    }

    Task Render(string scope) => scope switch
    {
        "application" => _renderer.Render([_application], _targetDirectory, _output, _error),
        "module" => _renderer.Render(_module, _context, _targetDirectory, _output, _error),
        "feature" => _renderer.Render(_feature, _context, _targetDirectory, _output, _error, module: "Billing"),
        _ => _renderer.Render(_blocked, _context, _targetDirectory, _output, _error, module: "Billing", feature: "Invoices")
    };

    void AssertFailure(Exception exception, QuerySyntax query, UnsupportedQueryIntentReason reason, SourceLocation location)
    {
        var failure = Assert.IsType<RenderingFailed>(exception);
        var rejection = Assert.IsType<UnsupportedQueryIntent>(Assert.Single(failure.Failures));
        rejection.QueryName.ShouldEqual(query.Name);
        rejection.ReadModel.ShouldEqual(query.ReturnType.Name);
        rejection.Reason.ShouldEqual(reason);
        rejection.Location.ShouldEqual(location);
        rejection.SlicePath.ShouldEqual("Billing.Invoices.Summary");
        rejection.Message.ShouldContain("STAGE-CRATIS-QUERY-001");
        rejection.Message.ShouldNotContain("Array.Empty");
        _error.ToString().ShouldContain(rejection.Message);
        _output.ToString().ShouldNotContain("Rendering complete.");
        _codeOutput.FailureMarkerWasWritten.ShouldBeTrue();
        _blocked.Specifications.ShouldNotBeEmpty();
        _codeOutput.Files.ShouldNotContain(file => file.RelativePath.Contains("Summary", StringComparison.Ordinal));
    }
}
