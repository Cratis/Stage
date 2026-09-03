// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Projections;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.for_CratisRenderer;
using Cratis.Stage.Rendering.Cratis.Renderers;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_StateViewSliceRenderer;

/// <summary>
/// A key written on a <c>from</c> block, or on one of its events, is what decides which document the event
/// updates. Chronicle seeds every <c>From</c> with the event source id and only overwrites it from a class-level
/// <c>[FromEvent]</c>'s key — never from <c>[Key]</c>, which it reads solely to identify a child — so a read
/// model carrying only <c>[Key]</c> keeps routing on the event source id however the projection is written.
/// </summary>
/// <remarks>
/// The key is deliberately declared on the <c>from</c> blocks here rather than on the projection: the kernel's
/// visitor never reads <see cref="ProjectionSyntax.Key"/>, so a projection-level key routes nothing and the two
/// agree on the event source id. This covers the case where they would otherwise disagree.
/// </remarks>
public class when_a_from_block_declares_its_own_key : Specification
{
    ApplicationSet _applicationSet = null!;
    RenderedFile _file = null!;
    IReadOnlyList<string> _compilationErrors = null!;

    void Establish()
    {
        var placed = Event("OrderPlaced", "orderNumber", "customerRef");
        var cancelled = Event("OrderCancelled", "orderNumber", "cancellationRef");

        // Key on the block itself, which every event it names inherits.
        var placedFrom = new FromSyntax(
            [new EventSpecSyntax("OrderPlaced", null, SourceLocation.Start)],
            new ExpressionKeySyntax(new PathExpressionSyntax("orderNumber", SourceLocation.Start), SourceLocation.Start),
            null,
            [Set("orderNumber", "orderNumber")],
            SourceLocation.Start);

        // A key on the event itself, which the kernel resolves ahead of any block key.
        var cancelledFrom = new FromSyntax(
            [new EventSpecSyntax("OrderCancelled", new PathExpressionSyntax("cancellationRef", SourceLocation.Start), SourceLocation.Start)],
            null,
            null,
            [Set("cancellationRef", "cancellationRef")],
            SourceLocation.Start);

        var projection = new ProjectionSyntax(
            "OrderRouting", "OrderRouting", null, AutoMapMode.Enabled, null, [placedFrom, cancelledFrom], SourceLocation.Start);

        var slice = new SliceSyntax(
            SliceType.StateView, "OrderRouting", [placed, cancelled], [], [], [projection], [], [], [], [], [], SourceLocation.Start);

        var feature = new FeatureSyntax("Orders", [], [slice], SourceLocation.Start);
        _applicationSet = new ApplicationSet(
            [new ApplicationSyntax([], [], [], [new ModuleSyntax("Sales", [], [feature], SourceLocation.Start)], SourceLocation.Start)]);
    }

    void Because()
    {
        _file = new StateViewSliceRenderer().Render(_applicationSet.Slices.Single(), _applicationSet, "CratisApp");
        _compilationErrors = RenderedOutput.Errors([_file]);
    }

    [Fact] void should_render_output_that_compiles() => string.Join(Environment.NewLine, _compilationErrors).ShouldEqual(string.Empty);

    [Fact] void should_carry_a_block_key_onto_the_read_models_from_event() =>
        _file.Content.ShouldContain("[FromEvent<OrderPlaced>(key: nameof(OrderPlaced.OrderNumber))]");

    [Fact] void should_carry_an_event_key_onto_the_read_models_from_event() =>
        _file.Content.ShouldContain("[FromEvent<OrderCancelled>(key: nameof(OrderCancelled.CancellationRef))]");

    // The shape the defect produced: a bare attribute, which Chronicle leaves routing on the event source id.
    [Fact] void should_not_leave_a_keyed_event_routing_on_the_event_source_id() =>
        _file.Content.ShouldNotContain("[FromEvent<OrderPlaced>]");

    [Fact] void should_report_that_the_inline_keys_differ_from_the_models_own_key() =>
        _file.Diagnostics.Any(diagnostic => diagnostic.Contains("declare(s) a key", StringComparison.Ordinal)).ShouldBeTrue();

    static EventSyntax Event(string name, params string[] properties) =>
        new(
            name,
            [.. properties.Select(property =>
                new PropertySyntax(property, new TypeRefSyntax("String", false, false, SourceLocation.Start), SourceLocation.Start))],
            SourceLocation.Start);

    static SetMappingSyntax Set(string property, string source) =>
        new(property, new PathExpressionSyntax(source, SourceLocation.Start), SourceLocation.Start);
}
