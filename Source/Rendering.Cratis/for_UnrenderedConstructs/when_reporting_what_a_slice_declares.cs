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

    [Fact] void should_report_every_family_it_declares() => _diagnostics.Count.ShouldEqual(11);
    [Fact] void should_note_every_family_in_the_emitted_file() =>
        _builder.ToString().Split('\n').Count(line => line.StartsWith("// TODO:", StringComparison.Ordinal)).ShouldEqual(11);
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
    [Fact] void should_report_the_reactor() =>
        _diagnostics.ShouldContain(
            "Slice 'Summary' declares 1 reactor declaration(s) with no rendered equivalent — nothing reacts to the events in the " +
            "rendered application.");
    [Fact] void should_report_the_queries() =>
        _diagnostics.ShouldContain(
            "Slice 'Summary' declares 1 query declaration(s) with no rendered equivalent — the read model carries the fixed " +
            "all/by-id pair instead, guarded by the union of the authorization declared by the queries that read it.");
    [Fact] void should_report_the_query_performer() =>
        _diagnostics.ShouldContain(
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
    [Fact] void should_report_the_specifications() =>
        _diagnostics.ShouldContain(
            "Slice 'Summary' declares 1 specification declaration(s) with no rendered equivalent — no specs are rendered for the " +
            "generated application.");
}
