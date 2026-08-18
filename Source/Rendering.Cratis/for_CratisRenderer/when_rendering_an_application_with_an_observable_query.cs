// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_CratisRenderer.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisRenderer;

public class when_rendering_an_application_with_an_observable_query : an_application_with_an_observable_query
{
    IReadOnlyList<string> _compilationErrors = null!;

    async Task Because()
    {
        await _renderer.Render([_application], _targetDirectory, _output, _error);
        _compilationErrors = RenderedOutput.Errors(_codeOutput.Files);
    }

    // Asserted as joined text rather than an empty collection so a failure names the compilation errors. This is
    // the assertion that decides whether the live query is real: Observe and ObserveById have to resolve on
    // IMongoCollection<T>, and ISubject has to be in scope, or the rendered application does not build.
    [Fact] void should_render_output_that_compiles() =>
        string.Join(Environment.NewLine, _compilationErrors).ShouldEqual(string.Empty);

    [Fact] void should_render_the_observable_collection_query_as_a_live_read_of_the_collection() =>
        SliceContent("Summary").ShouldContain("public static ISubject<IEnumerable<InvoiceSummary>> LiveOutstanding(IMongoCollection<InvoiceSummary> collection) => collection.Observe();");

    [Fact] void should_render_the_observable_by_query_as_a_live_read_of_one_instance() =>
        SliceContent("Summary").ShouldContain("public static ISubject<InvoiceSummary> LiveForInvoice(IMongoCollection<InvoiceSummary> collection, InvoiceNumber number) => collection.ObserveById(number);");

    [Fact] void should_import_the_namespace_the_subject_lives_in() =>
        SliceContent("Summary").ShouldContain("using System.Reactive.Subjects;");

    [Fact] void should_leave_the_query_the_document_does_not_mark_observable_a_one_shot_read() =>
        SliceContent("Summary").ShouldContain("public static IQueryable<InvoiceSummary> Outstanding(IMongoCollection<InvoiceSummary> collection) => collection.AsQueryable();");

    [Fact] void should_not_report_an_observable_query_as_unrendered() =>
        _error.ToString().ShouldNotContain("Slice 'Summary' declares");

    string SliceContent(string slice) =>
        _codeOutput.Files.Single(file => file.RelativePath.EndsWith(Path.Combine(slice, $"{slice}.cs"), StringComparison.Ordinal)).Content;
}
