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
/// Two things a <c>children</c> block can say that nothing carries into the generated code, both reachable from a
/// Screenplay program the parser accepts, and both silent until now.
/// </summary>
/// <remarks>
/// A <c>from</c> without <c>parent</c> leaves Chronicle to guess which document the children hang off: it looks
/// for a property literally named <c>Id</c> on the read model — never <c>[Key]</c> — and otherwise falls through
/// to the event source id, so children can land under a parent nobody chose without anything failing. And
/// <c>ParseChildren</c> parses its body in nested scope, so <c>clear with</c> is as valid inside <c>children</c>
/// as inside <c>nested</c>, yet only a nested type has a class-level <c>[ClearWith]</c> Chronicle reads.
/// </remarks>
public class when_a_children_block_leaves_the_parent_unsaid : Specification
{
    ApplicationSet _applicationSet = null!;
    RenderedFile _file = null!;
    IReadOnlyList<string> _compilationErrors = null!;

    void Establish()
    {
        var opened = Event("BasketOpened", "basketNumber");
        var lineAdded = Event("BasketLineAdded", "lineNumber", "sku");
        var lineCleared = Event("BasketLineCleared", "lineNumber");

        var root = new FromSyntax(
            [new EventSpecSyntax("BasketOpened", null, SourceLocation.Start)],
            null,
            null,
            [Set("basketNumber", "basketNumber")],
            SourceLocation.Start);

        // No 'parent' anywhere in this children block, and a 'clear with' the parser accepts here.
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
            [childFrom, new ClearWithSyntax("BasketLineCleared", SourceLocation.Start)],
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
            SliceType.StateView, "BasketSummary", [opened, lineAdded, lineCleared], [], [], [projection], [], [], [], [], [], SourceLocation.Start);

        var feature = new FeatureSyntax("Baskets", [], [slice], SourceLocation.Start);
        _applicationSet = new ApplicationSet(
            [new ApplicationSyntax([], [], [], [new ModuleSyntax("Sales", [], [feature], SourceLocation.Start)], SourceLocation.Start)]);
    }

    void Because()
    {
        _file = new StateViewSliceRenderer().Render(_applicationSet.Slices.Single(), _applicationSet, "CratisApp");
        _compilationErrors = RenderedOutput.Errors([_file]);
    }

    [Fact] void should_render_output_that_compiles() => string.Join(Environment.NewLine, _compilationErrors).ShouldEqual(string.Empty);

    [Fact] void should_report_that_the_children_block_says_nothing_about_the_parent() =>
        _file.Diagnostics.ShouldContain(
            "The 'from' on 'BasketLineAdded' in children record 'BasketSummaryLines' declares no 'parent' — Chronicle infers the parent " +
            "from an 'Id' property on the read model and otherwise attaches the children on the event source id, so declare " +
            "'parent' to say which property identifies the parent.");

    [Fact] void should_report_the_clear_with_the_child_cannot_carry() =>
        _file.Diagnostics.ShouldContain(
            "Children record 'BasketSummaryLines' declares 1 clear with block(s) whose meaning on a child type is not established — they are not rendered.");

    [Fact] void should_flag_the_clear_with_in_the_file() =>
        _file.Content.ShouldContain("// TODO: 1 clear with block(s) not yet rendered — their meaning on a child type is not established");

    // The shape the defect produced: the clear with vanishing with nothing said about it.
    [Fact] void should_not_render_a_clear_with_attribute_on_the_child_record() => _file.Content.ShouldNotContain("ClearWith<");

    static EventSyntax Event(string name, params string[] properties) =>
        new(
            name,
            [.. properties.Select(property =>
                new PropertySyntax(property, new TypeRefSyntax("String", false, false, SourceLocation.Start), SourceLocation.Start))],
            SourceLocation.Start);

    static SetMappingSyntax Set(string property, string source) =>
        new(property, new PathExpressionSyntax(source, SourceLocation.Start), SourceLocation.Start);
}
