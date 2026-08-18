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
/// A read model whose slice declares a query the document marks <c>observable</c> — a live read that keeps
/// pushing as the read model changes, rather than answering once.
/// </summary>
/// <remarks>
/// This one was rendered as its own opposite. An <c>observable</c> query became the same one-shot method a
/// plain query does, and nothing anywhere said so — not the file, not a diagnostic — so the rendered
/// application looked exactly like the one the document asked for and was not it.
/// </remarks>
public class and_one_of_them_is_observable : Specification
{
    static QuerySyntax Collection(string name, bool observable) =>
        new(
            name,
            new TypeRefSyntax("InvoiceSummary", true, false, SourceLocation.Start),
            null,
            [],
            null,
            SourceLocation.Start,
            IsObservable: observable);

    static QuerySyntax By(string name, string parameter, bool observable) =>
        new(
            name,
            new TypeRefSyntax("InvoiceSummary", false, false, SourceLocation.Start),
            new QueryParameterSyntax(parameter, new TypeRefSyntax("String", false, false, SourceLocation.Start), SourceLocation.Start),
            [],
            null,
            SourceLocation.Start,
            IsObservable: observable);

    static QuerySyntax Single(string name, bool observable) =>
        new(
            name,
            new TypeRefSyntax("InvoiceSummary", false, false, SourceLocation.Start),
            null,
            [],
            null,
            SourceLocation.Start,
            IsObservable: observable);

    static string Render(params QuerySyntax[] queries)
    {
        var builder = new CSharpCodeBuilder();
        QueryRenderer.Render(builder, "InvoiceSummary", "string", "id", queries);
        return builder.ToString();
    }

    [Fact] void should_return_a_subject_of_the_collection_for_an_observable_collection_query() =>
        Render(Collection("LiveInvoices", observable: true))
            .ShouldContain("public static ISubject<IEnumerable<InvoiceSummary>> LiveInvoices(IMongoCollection<InvoiceSummary> collection) => collection.Observe();");

    [Fact] void should_return_a_subject_of_one_instance_for_an_observable_query_naming_an_identifying_parameter() =>
        Render(By("LiveForInvoice", "invoiceNumber", observable: true))
            .ShouldContain("public static ISubject<InvoiceSummary> LiveForInvoice(IMongoCollection<InvoiceSummary> collection, string invoiceNumber) => collection.ObserveById(invoiceNumber);");

    [Fact] void should_return_a_subject_of_one_instance_for_an_observable_query_answering_the_model_itself() =>
        Render(Single("LiveSummary", observable: true))
            .ShouldContain("public static ISubject<InvoiceSummary> LiveSummary(IMongoCollection<InvoiceSummary> collection, string id) => collection.ObserveById(id);");

    // Returned directly. A live query behind a Task has to be awaited before anything can subscribe to it, which
    // is the one-shot read the document asked it not to be.
    [Fact] void should_not_wrap_a_live_query_in_a_task() =>
        Render(Collection("LiveInvoices", observable: true)).ShouldNotContain("Task<ISubject");

    [Fact] void should_import_the_namespace_the_subject_lives_in() =>
        Render(Collection("LiveInvoices", observable: true)).ShouldContain("using System.Reactive.Subjects;");

    [Fact] void should_not_import_it_when_nothing_the_slice_declares_is_observable() =>
        Render(Collection("Invoices", observable: false)).ShouldNotContain("using System.Reactive.Subjects;");

    [Fact] void should_leave_a_query_the_document_does_not_mark_observable_a_one_shot_read() =>
        Render(Collection("Invoices", observable: false))
            .ShouldContain("public static IQueryable<InvoiceSummary> Invoices(IMongoCollection<InvoiceSummary> collection) => collection.AsQueryable();");

    [Fact] void should_leave_a_by_query_the_document_does_not_mark_observable_a_one_shot_read() =>
        Render(By("ForInvoice", "invoiceNumber", observable: false))
            .ShouldContain("public static Task<InvoiceSummary?> ForInvoice(IReadModels readModels, string invoiceNumber)");

    [Fact] void should_render_each_query_as_what_the_document_declares_it_to_be() =>
        Render(Collection("Invoices", observable: false), Collection("LiveInvoices", observable: true))
            .ShouldContain("IQueryable<InvoiceSummary> Invoices(");

    [Fact] void should_still_treat_an_observable_query_as_fully_rendered() =>
        QueryRenderer.IsFullyRendered(Collection("LiveInvoices", observable: true)).ShouldBeTrue();
}
