// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.for_UnrenderedConstructs.given;
using Cratis.Stage.Rendering.Cratis.Renderers;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_UnrenderedConstructs;

/// <summary>
/// The same slice, rendered by a renderer that does emit the read model its query returns — so the query gets a
/// method, and what is reported about it is only the narrowing that method does not have.
/// </summary>
public class when_the_renderer_emits_the_read_model_a_query_returns : a_slice_declaring_every_family
{
    readonly CSharpCodeBuilder _builder = new();
    readonly List<string> _diagnostics = [];

    void Because() =>
        UnrenderedConstructs.Report(_builder, _slice, RenderedConstructs.ReadModel, _diagnostics, "InvoiceSummary");

    [Fact] void should_report_the_narrowing_its_rendered_method_does_not_have() =>
        _diagnostics.ShouldContain(
            "Slice 'Summary' declares 1 query declaration(s) with no rendered equivalent — it is rendered as a method " +
            "reading the whole read model, without the narrowing the document states.");

    [Fact] void should_report_the_performer_holding_the_body_it_does_not_render() =>
        _diagnostics.ShouldContain(
            "Slice 'Summary' declares 1 query performer declaration(s) with no rendered equivalent — the query logic is not rendered.");

    [Fact] void should_not_report_the_query_as_having_no_method() =>
        _diagnostics.Exists(diagnostic => diagnostic.StartsWith("Query 'All'", StringComparison.Ordinal)).ShouldBeFalse();
}
