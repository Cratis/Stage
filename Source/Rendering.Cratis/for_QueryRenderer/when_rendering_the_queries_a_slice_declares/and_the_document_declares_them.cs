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
/// A read model whose slice declares its own queries — a collection, and one reading a single instance by an
/// identifying parameter.
/// </summary>
/// <remarks>
/// The names are the whole point. A read model used to receive a fixed pair whatever the document said, so a
/// declared query was neither rendered nor mentioned and two methods nobody wrote appeared in its place.
/// </remarks>
public class and_the_document_declares_them : Specification
{
    static QuerySyntax Collection(string name) =>
        new(name, new TypeRefSyntax("InvoiceSummary", true, false, SourceLocation.Start), null, [], null, SourceLocation.Start);

    static QuerySyntax By(string name, string parameter) =>
        new(
            name,
            new TypeRefSyntax("InvoiceSummary", false, false, SourceLocation.Start),
            new QueryParameterSyntax(parameter, new TypeRefSyntax("String", false, false, SourceLocation.Start), SourceLocation.Start),
            [],
            null,
            SourceLocation.Start);

    static string Render(params QuerySyntax[] queries)
    {
        var builder = new CSharpCodeBuilder();
        QueryRenderer.Render(builder, "InvoiceSummary", "string", "id", queries);
        return builder.ToString();
    }

    [Fact] void should_name_a_collection_query_after_the_document() =>
        Render(Collection("OverdueInvoices")).ShouldContain("IQueryable<InvoiceSummary> OverdueInvoices(");

    [Fact] void should_render_every_declared_query() =>
        Render(Collection("All"), Collection("Mine")).ShouldContain("Mine(");

    [Fact] void should_read_one_instance_when_the_query_names_an_identifying_parameter() =>
        Render(By("ForInvoice", "invoiceNumber")).ShouldContain("Task<InvoiceSummary?> ForInvoice(IReadModels readModels, string invoiceNumber)");

    [Fact] void should_not_invent_the_fixed_pair_when_the_document_declares_a_query() =>
        Render(Collection("OverdueInvoices")).ShouldNotContain("InvoiceSummaryById");

    [Fact] void should_still_give_a_read_model_with_no_declared_query_a_way_in() =>
        Render().ShouldContain("InvoiceSummaryById");

    [Fact] void should_treat_a_performer_backed_query_as_not_fully_rendered() =>
        QueryRenderer.IsFullyRendered(
            Collection("Bespoke") with { Performer = new PerformerSyntax(new FileReferenceSyntax("Q.cs", SourceLocation.Start), null, SourceLocation.Start) })
            .ShouldBeFalse();

    [Fact] void should_treat_a_plain_declared_query_as_fully_rendered() =>
        QueryRenderer.IsFullyRendered(Collection("All")).ShouldBeTrue();
}
