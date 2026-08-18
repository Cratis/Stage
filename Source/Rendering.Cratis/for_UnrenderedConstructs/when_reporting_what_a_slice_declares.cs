// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.for_UnrenderedConstructs.given;
using Cratis.Stage.Rendering.Cratis.Renderers;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_UnrenderedConstructs;

public class when_reporting_what_a_slice_declares : a_slice_declaring_every_family
{
    readonly CSharpCodeBuilder _builder = new();
    readonly List<string> _diagnostics = [];

    void Because() => UnrenderedConstructs.Report(_builder, _slice, RenderedConstructs.None, _diagnostics);

    [Fact] void should_report_every_family_it_declares() => _diagnostics.Count.ShouldEqual(9);
    [Fact] void should_note_every_family_in_the_emitted_file() =>
        _builder.ToString().Split('\n').Count(line => line.StartsWith("// TODO:", StringComparison.Ordinal)).ShouldEqual(9);
    [Fact] void should_report_the_command() =>
        _diagnostics.ShouldContain(
            "Slice 'Summary' declares 1 command declaration(s) with no rendered equivalent — neither its input, the events it " +
            "produces nor its authorization is rendered.");
    [Fact] void should_report_the_projection() =>
        _diagnostics.ShouldContain(
            "Slice 'Summary' declares 1 projection declaration(s) with no rendered equivalent — no read model is rendered for it.");
    [Fact] void should_report_the_read_model() =>
        _diagnostics.ShouldContain(
            "Slice 'Summary' declares 1 readmodel declaration(s) with no rendered equivalent — nothing in the rendered " +
            "application holds the state it declares.");
    [Fact] void should_report_the_reducer() =>
        _diagnostics.ShouldContain(
            "Slice 'Summary' declares 1 reducer declaration(s) with no rendered equivalent — the read model it builds is " +
            "never populated in the rendered application.");
    [Fact] void should_report_the_reaction() =>
        _diagnostics.ShouldContain(
            "Slice 'Summary' declares 1 reaction declaration(s) with no rendered equivalent — nothing reacts to the events in the " +
            "rendered application.");

    // This renderer emits no read model, so there is nothing for a query's method to live on and the query is
    // reported whole — by name, and with what it returns, since that is what a reader needs to see it is missing.
    [Fact] void should_report_the_query_it_renders_no_method_for() =>
        _diagnostics.ShouldContain(
            "Query 'All' returns 'InvoiceSummary', which is not the read model rendered here — no method is rendered for it, " +
            "since rendering one against the read model that is here would answer with a read model the document does not ask " +
            "it for.");

    // Reporting the narrowing a missing method does not have would describe a method that is not there.
    [Fact] void should_not_report_the_narrowing_of_a_query_it_renders_no_method_for() =>
        _diagnostics.ShouldNotContain(
            "Slice 'Summary' declares 1 query declaration(s) with no rendered equivalent — it is rendered as a method " +
            "reading the whole read model, without the narrowing the document states.");
    [Fact] void should_not_report_the_performer_of_a_query_it_renders_no_method_for() =>
        _diagnostics.ShouldNotContain(
            "Slice 'Summary' declares 1 query performer declaration(s) with no rendered equivalent — the query logic is not rendered.");
    [Fact] void should_report_the_constraints() =>
        _diagnostics.ShouldContain(
            "Slice 'Summary' declares 1 constraint declaration(s) with no rendered equivalent — uniqueness is not enforced in the " +
            "rendered application.");
    [Fact] void should_report_the_screens() =>
        _diagnostics.ShouldContain(
            "Slice 'Summary' declares 1 screen declaration(s) with no rendered equivalent — the rendered application has no user interface.");
    [Fact] void should_report_the_captures() =>
        _diagnostics.ShouldContain(
            "Slice 'Summary' declares 1 capture declaration(s) with no rendered equivalent — no ingestion of the captured source is rendered.");

    // Specifications are not in this list any more: each one is rendered into its own file, or says on its own
    // why it could not be. Counting them here would report a rendered specification as dropped.
    [Fact] void should_not_report_the_specifications() =>
        _diagnostics.ShouldNotContain(
            "Slice 'Summary' declares 1 specification declaration(s) with no rendered equivalent — no specs are rendered for the " +
            "generated application.");
}
