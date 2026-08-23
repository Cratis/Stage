// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.Renderers;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_QueryRenderer.when_rendering_the_queries_a_slice_declares;

/// <summary>
/// A slice declaring queries for more than one read model — the shape of the invoicing sample's dashboard, where
/// one query returns the summary the slice projects and another returns the overdue list.
/// </summary>
/// <remarks>
/// Every declared query used to be rendered against the read model the slice's first projection builds, whatever
/// the query's return type said. A query returning <c>OverdueInvoices</c> came out reading <c>InvoiceSummary</c>:
/// a different read model, put there by nobody, named in no report — the rendered application answered the
/// document's question with someone else's data and looked correct doing it.
/// </remarks>
public class and_they_return_different_read_models : Specification
{
    static QuerySyntax Collection(string name, string readModel, bool observable = false) =>
        new(
            name,
            new TypeRefSyntax(readModel, true, false, SourceLocation.Start),
            null,
            [],
            null,
            SourceLocation.Start,
            IsObservable: observable);

    static QuerySyntax By(string name, string readModel, string parameter) =>
        new(
            name,
            new TypeRefSyntax(readModel, false, false, SourceLocation.Start),
            new QueryParameterSyntax(parameter, new TypeRefSyntax("String", false, false, SourceLocation.Start), SourceLocation.Start),
            [],
            null,
            SourceLocation.Start);

    static string Render(params QuerySyntax[] queries)
    {
        var builder = new CSharpCodeBuilder();
        QueryRenderer.Render(builder, "InvoiceSummary", "string", "id", queries, new ApplicationSet([]), []);
        return builder.ToString();
    }

    [Fact] void should_render_the_query_whose_return_type_names_this_read_model() =>
        Render(Collection("GetInvoiceSummary", "InvoiceSummary"), Collection("GetOverdueInvoices", "OverdueInvoices"))
            .ShouldContain("public static IQueryable<InvoiceSummary> GetInvoiceSummary(IMongoCollection<InvoiceSummary> collection) => collection.AsQueryable();");

    [Fact] void should_not_render_a_method_for_a_query_returning_another_read_model() =>
        Render(Collection("GetInvoiceSummary", "InvoiceSummary"), Collection("GetOverdueInvoices", "OverdueInvoices"))
            .ShouldNotContain("GetOverdueInvoices");

    // The defect itself: the method was rendered, against the wrong read model, and nothing said so.
    [Fact] void should_not_answer_another_read_models_query_with_this_read_model() =>
        Render(Collection("GetOverdueInvoices", "OverdueInvoices")).ShouldNotContain("IQueryable<InvoiceSummary> GetOverdueInvoices");

    [Fact] void should_give_the_read_model_a_way_in_when_no_declared_query_returns_it() =>
        Render(Collection("GetOverdueInvoices", "OverdueInvoices")).ShouldContain("InvoiceSummaryById");

    [Fact] void should_keep_the_collection_shape_of_its_own_query() =>
        Render(Collection("GetOverdueInvoices", "OverdueInvoices"), Collection("Invoices", "InvoiceSummary"))
            .ShouldContain("public static IQueryable<InvoiceSummary> Invoices(IMongoCollection<InvoiceSummary> collection) => collection.AsQueryable();");

    [Fact] void should_keep_the_by_shape_of_its_own_query() =>
        Render(Collection("GetOverdueInvoices", "OverdueInvoices"), By("ForInvoice", "InvoiceSummary", "invoiceNumber"))
            .ShouldContain("public static Task<InvoiceSummary?> ForInvoice(IReadModels readModels, string invoiceNumber)");

    [Fact] void should_keep_the_observable_shape_of_its_own_query() =>
        Render(Collection("GetOverdueInvoices", "OverdueInvoices"), Collection("LiveInvoices", "InvoiceSummary", observable: true))
            .ShouldContain("public static ISubject<IEnumerable<InvoiceSummary>> LiveInvoices(IMongoCollection<InvoiceSummary> collection) => collection.Observe();");

    [Fact] void should_import_the_observable_namespace_for_its_own_live_query() =>
        Render(Collection("LiveInvoices", "InvoiceSummary", observable: true)).ShouldContain("using System.Reactive.Subjects;");

    // Nothing live is rendered here, so the import would be an unused one on a file whose live query lives
    // somewhere else entirely.
    [Fact] void should_not_import_the_observable_namespace_for_another_read_models_live_query() =>
        Render(Collection("LiveOverdue", "OverdueInvoices", observable: true), Collection("Invoices", "InvoiceSummary"))
            .ShouldNotContain("using System.Reactive.Subjects;");

    [Fact] void should_attribute_a_query_to_the_read_model_its_return_type_names() =>
        QueryRenderer.Reads(Collection("Invoices", "InvoiceSummary"), "InvoiceSummary").ShouldBeTrue();

    [Fact] void should_not_attribute_a_query_to_a_read_model_its_return_type_does_not_name() =>
        QueryRenderer.Reads(Collection("GetOverdueInvoices", "OverdueInvoices"), "InvoiceSummary").ShouldBeFalse();

    [Fact] void should_attribute_a_query_answering_one_instance_the_same_way() =>
        QueryRenderer.Reads(By("ForInvoice", "InvoiceSummary", "invoiceNumber"), "InvoiceSummary").ShouldBeTrue();

    [Fact] void should_render_a_method_for_a_query_returning_the_read_model_that_is_rendered() =>
        QueryRenderer.HasRenderedMethod(Collection("Invoices", "InvoiceSummary"), "InvoiceSummary").ShouldBeTrue();

    [Fact] void should_render_no_method_for_a_query_returning_another_read_model() =>
        QueryRenderer.HasRenderedMethod(Collection("GetOverdueInvoices", "OverdueInvoices"), "InvoiceSummary").ShouldBeFalse();

    [Fact] void should_render_no_method_at_all_when_no_read_model_is_rendered() =>
        QueryRenderer.HasRenderedMethod(Collection("Invoices", "InvoiceSummary"), null).ShouldBeFalse();
}
