// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.for_StateViewSliceRenderer.given;
using Cratis.Stage.Rendering.Cratis.Renderers;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_StateViewSliceRenderer;

/// <summary>
/// The query names a read model nothing in the slice builds, so there is no type for its method to answer with.
/// </summary>
/// <remarks>
/// Two ways to keep the file honest were open here: emit the method against the name the document states and let
/// it fail to compile, or leave it out and say so. Leaving it out is what is done — a method rendered against a
/// type the application does not define is a promise the file cannot keep either, and it takes the whole file
/// down with it. What is never done is the third option, which is what used to happen: render it against whatever
/// read model is at hand, which compiles, reads correctly, and answers the wrong question.
/// </remarks>
public class when_a_querys_return_type_resolves_to_no_read_model : a_slice_whose_query_returns_a_read_model_nothing_builds
{
    RenderedFile _file = null!;
    StateViewSliceRenderer _renderer = null!;

    void Establish() => _renderer = new StateViewSliceRenderer();

    void Because() => _file = _renderer.Render(_dashboardSlice, _applicationSet, "CratisApp");

    [Fact] void should_not_render_a_method_for_it() =>
        _file.Content.Contains("GetOverdueInvoices(", StringComparison.Ordinal).ShouldBeFalse();

    [Fact] void should_not_answer_it_with_the_read_model_that_is_there() =>
        _file.Content.ShouldNotContain("IQueryable<InvoiceSummary> GetOverdueInvoices");

    [Fact] void should_not_render_a_method_against_a_type_the_application_does_not_define() =>
        _file.Content.ShouldNotContain("IQueryable<OverdueInvoices>");

    [Fact] void should_report_it() =>
        _file.Diagnostics.ShouldContain(
            "Query 'GetOverdueInvoices' returns 'OverdueInvoices', which is not the read model rendered here — no method is " +
            "rendered for it, since rendering one against the read model that is here would answer with a read model the " +
            "document does not ask it for.");

    [Fact] void should_note_it_in_the_emitted_file() =>
        _file.Content.ShouldContain(
            "// TODO: query 'GetOverdueInvoices' not rendered — it returns 'OverdueInvoices', which is not the read model rendered here");

    // Nothing the document states reads this read model, so it keeps the way in that a read model with no query of
    // its own has always been given rather than being rendered with no read surface at all.
    [Fact] void should_still_give_the_rendered_read_model_a_way_in() =>
        _file.Content.ShouldContain("public static IQueryable<InvoiceSummary> AllInvoiceSummaries(IMongoCollection<InvoiceSummary> collection) => collection.AsQueryable();");
}
