// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Projections;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.Renderers;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_StateViewSliceRenderer;

/// <summary>
/// The blocks that still have no rendering: <c>children</c> and <c>nested</c> each project into a child record
/// type nothing generates yet, a <c>clear with</c> is only valid inside a <c>nested</c> block and is only read by
/// Chronicle on a nested type, and a composite key has no model-bound equivalent at all. All four have to stay
/// visible in the file and in the diagnostics rather than disappear now that their neighbors render.
/// </summary>
public class when_the_projection_declares_what_is_still_unrendered : Specification
{
    ApplicationSet _applicationSet = null!;
    RenderedFile _file = null!;

    void Establish()
    {
        var lineAdded = new EventSyntax(
            "OrderLineAdded",
            [
                new PropertySyntax("orderNumber", new TypeRefSyntax("String", false, false, SourceLocation.Start), SourceLocation.Start),
                new PropertySyntax("sku", new TypeRefSyntax("String", false, false, SourceLocation.Start), SourceLocation.Start),
            ],
            SourceLocation.Start);

        var from = new FromSyntax(
            [new EventSpecSyntax("OrderLineAdded", null, SourceLocation.Start)],
            null,
            null,
            [new SetMappingSyntax("orderNumber", new PathExpressionSyntax("orderNumber", SourceLocation.Start), SourceLocation.Start)],
            SourceLocation.Start);

        var children = new ChildrenSyntax(
            "lines",
            new PathExpressionSyntax("sku", SourceLocation.Start),
            AutoMapMode.Inherit,
            [],
            SourceLocation.Start);

        var nested = new NestedSyntax("shipping", AutoMapMode.Inherit, [], SourceLocation.Start);

        var clearWith = new ClearWithSyntax("OrderArchived", SourceLocation.Start);

        var compositeKey = new CompositeKeySyntax(
            "OrderLineKey",
            [new KeyPartSyntax("orderNumber", new PathExpressionSyntax("orderNumber", SourceLocation.Start), SourceLocation.Start)],
            SourceLocation.Start);

        var projection = new ProjectionSyntax(
            "OrderLines",
            "OrderLines",
            null,
            AutoMapMode.Enabled,
            compositeKey,
            [from, children, nested, clearWith],
            SourceLocation.Start);

        var slice = new SliceSyntax(
            SliceType.StateView, "OrderLines", [lineAdded], [], [], [projection], [], [], [], [], [], SourceLocation.Start);

        var feature = new FeatureSyntax("Orders", [], [slice], SourceLocation.Start);
        var module = new ModuleSyntax("Sales", [], [feature], SourceLocation.Start);
        _applicationSet = new ApplicationSet([new ApplicationSyntax([], [], [], [module], SourceLocation.Start)]);
    }

    void Because() => _file = new StateViewSliceRenderer().Render(_applicationSet.Slices.Single(), _applicationSet, "CratisApp");

    [Fact] void should_flag_the_unrendered_blocks_in_the_file() =>
        _file.Content.ShouldContain("// TODO: 1 children, 1 nested, 1 clear with block(s) not yet rendered");
    [Fact] void should_report_the_unrendered_blocks_as_a_diagnostic() =>
        _file.Diagnostics.ShouldContain(
            "Projection 'OrderLines' declares 1 children, 1 nested, 1 clear with block(s) that belong to a nested or child record type nothing generates yet — they are not rendered.");
    [Fact] void should_report_the_composite_key_as_a_diagnostic() =>
        _file.Diagnostics.ShouldContain("Composite key 'OrderLineKey' has no model-bound equivalent — the read model is rendered without a key.");

    // A 'clear with' on a root read model is a silent no-op in Chronicle: the attribute is only read on a nested
    // type. Emitting it would look like it worked, so the renderer must not emit it at all.
    [Fact] void should_not_render_a_clear_with_attribute_on_the_root_read_model() =>
        _file.Content.ShouldNotContain("ClearWith<");
}
