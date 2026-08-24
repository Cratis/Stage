// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.for_StateViewSliceRenderer.given;
using Cratis.Stage.Rendering.Cratis.Renderers;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_StateViewSliceRenderer;

/// <summary>
/// The dashboard slice declares a query per read model, and only the first projection's read model is rendered.
/// The query reading the other one has nowhere to land.
/// </summary>
/// <remarks>
/// It used to land anyway: <c>GetOverdueInvoices</c> was rendered as a method reading <c>InvoiceSummary</c>, the
/// read model that happened to be there. The name came from the document, the type came from somewhere else, and
/// nothing anywhere reported the swap — a caller reading the document would have found the query it asked for,
/// answering with data it never asked about.
/// </remarks>
public class when_a_query_returns_another_read_model_the_slice_builds : a_slice_with_two_read_models
{
    RenderedFile _file = null!;
    StateViewSliceRenderer _renderer = null!;

    void Establish() => _renderer = new StateViewSliceRenderer();

    void Because() => _file = _renderer.Render(_dashboardSlice, _applicationSet, "CratisApp");

    [Fact] void should_render_the_query_that_returns_the_rendered_read_model() =>
        _file.Content.ShouldContain(
            "public static async Task<InvoiceSummary?> GetInvoiceSummary(IReadModels readModels, string invoiceNumber) => " +
            "await readModels.GetInstanceById<InvoiceSummary>((EventSourceId)invoiceNumber);");

    [Fact] void should_not_render_a_method_for_the_query_that_returns_the_other_one() =>
        _file.Content.ShouldNotContain("IQueryable<InvoiceSummary> GetOverdueInvoices");

    [Fact] void should_not_render_the_other_read_models_query_against_any_type() =>
        _file.Content.Contains("GetOverdueInvoices(", StringComparison.Ordinal).ShouldBeFalse();

    [Fact] void should_report_the_query_it_renders_no_method_for() =>
        _file.Diagnostics.ShouldContain(
            "Query 'GetOverdueInvoices' returns 'OverdueInvoices', which is not the read model rendered here — no method is " +
            "rendered for it, since rendering one against the read model that is here would answer with a read model the " +
            "document does not ask it for.");

    [Fact] void should_note_it_in_the_emitted_file() =>
        _file.Content.ShouldContain(
            "// TODO: query 'GetOverdueInvoices' not rendered — it returns 'OverdueInvoices', which is not the read model rendered here");

    // The query's policy belongs to its generated method. Rendering fewer methods must not quietly widen or
    // narrow it — the unguarded query on the other read model still says nothing here.
    [Fact] void should_keep_the_generated_method_guarded_by_its_querys_policy() =>
        _file.Content.ShouldContain("[Roles(\"Accountant\")]\n    public static async Task<InvoiceSummary?> GetInvoiceSummary");

    [Fact] void should_not_publish_the_read_model_to_everyone() => _file.Content.Contains("[AllowAnonymous]", StringComparison.Ordinal).ShouldBeFalse();
}
