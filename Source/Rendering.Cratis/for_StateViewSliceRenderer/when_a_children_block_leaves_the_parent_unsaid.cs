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
/// Preserves a supported child subscription while reporting inferred parent routing.
/// </summary>
public class when_a_children_block_leaves_the_parent_unsaid : Specification
{
    ApplicationSet _applicationSet = null!;
    RenderedFile _file = null!;

    void Establish()
    {
        var opened = Event("BasketOpened", "basketNumber");
        var lineAdded = Event("BasketLineAdded", "lineNumber", "sku");

        var root = new FromSyntax(
            [new EventSpecSyntax("BasketOpened", null, SourceLocation.Start)],
            null,
            null,
            [Set("basketNumber", "basketNumber")],
            SourceLocation.Start);

        // No 'parent' anywhere in this children block.
        var childFrom = new FromSyntax(
            [new EventSpecSyntax("BasketLineAdded", null, SourceLocation.Start)],
            new ExpressionKeySyntax(new PathExpressionSyntax("lineNumber", SourceLocation.Start), SourceLocation.Start),
            null,
            [Set("lineNumber", "lineNumber"), Set("sku", "sku")],
            SourceLocation.Start);

        var children = new ChildrenSyntax(
            "lines",
            new PathExpressionSyntax("lineNumber", SourceLocation.Start),
            AutoMapMode.Inherit,
            [childFrom],
            SourceLocation.Start);

        var projection = new ProjectionSyntax(
            "BasketSummary",
            "BasketSummary",
            null,
            AutoMapMode.Enabled,
            new ExpressionKeySyntax(new PathExpressionSyntax("basketNumber", SourceLocation.Start), SourceLocation.Start),
            [root, children],
            SourceLocation.Start);

        var slice = new SliceSyntax(
            SliceType.StateView, "BasketSummary", [opened, lineAdded], [], [], [projection], [], [], [], [], [], SourceLocation.Start);

        var feature = new FeatureSyntax("Baskets", [], [slice], SourceLocation.Start);
        _applicationSet = new ApplicationSet(
            [new ApplicationSyntax([], [], [], [new ModuleSyntax("Sales", [], [feature], SourceLocation.Start)], SourceLocation.Start)]);
    }

    void Because() => _file = new StateViewSliceRenderer().Render(_applicationSet.Slices.Single(), _applicationSet, "CratisApp");

    [Fact] void should_report_the_inferred_parent() => _file.Diagnostics.ShouldContain(
        "The 'from' on 'BasketLineAdded' in children record 'BasketSummaryLines' declares no 'parent' — Chronicle infers the parent " +
        "from an 'Id' property on the read model and otherwise attaches the children on the event source id, so declare " +
        "'parent' to say which property identifies the parent.");
    [Fact] void should_render_the_child_subscription() => _file.Content.ShouldContain("[ChildrenFrom<BasketLineAdded>");
    [Fact] void should_compile_supported_output() => RenderedOutput.Errors([_file]).ShouldBeEmpty();

    static EventSyntax Event(string name, params string[] properties) =>
        new(
            name,
            [.. properties.Select(property =>
                new PropertySyntax(property, new TypeRefSyntax("String", false, false, SourceLocation.Start), SourceLocation.Start))],
            SourceLocation.Start);

    static SetMappingSyntax Set(string property, string source) =>
        new(property, new PathExpressionSyntax(source, SourceLocation.Start), SourceLocation.Start);
}
