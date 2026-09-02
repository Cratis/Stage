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
/// <c>no automap</c> is an explicit instruction, and Chronicle defaults AutoMap to enabled whenever
/// <c>[NoAutoMap]</c> is absent. Failing to render it would not merely drop the instruction, it would invert it —
/// auto-mapping every name-matching event property the author wrote the block to exclude.
/// </summary>
public class when_the_projection_disables_auto_map : Specification
{
    ApplicationSet _applicationSet = null!;
    RenderedFile _disabled = null!;
    RenderedFile _enabled = null!;
    IReadOnlyList<string> _compilationErrors = null!;

    void Establish() => _applicationSet = new ApplicationSet([Application(AutoMapMode.Disabled), Application(AutoMapMode.Enabled)]);

    void Because()
    {
        var slices = _applicationSet.Slices.ToArray();
        var renderer = new StateViewSliceRenderer();
        _disabled = renderer.Render(slices[0], _applicationSet, "CratisApp");
        _enabled = renderer.Render(slices[1], _applicationSet, "CratisApp");
        _compilationErrors = RenderedOutput.Errors([_disabled]);
    }

    [Fact] void should_render_output_that_compiles() => string.Join(Environment.NewLine, _compilationErrors).ShouldEqual(string.Empty);
    [Fact] void should_render_no_auto_map_when_the_projection_disables_it() => _disabled.Content.ShouldContain("[NoAutoMap]");
    [Fact] void should_not_render_no_auto_map_when_the_projection_enables_it() => _enabled.Content.ShouldNotContain("[NoAutoMap]");

    static ApplicationSyntax Application(AutoMapMode autoMap)
    {
        var name = autoMap == AutoMapMode.Disabled ? "Strict" : "Loose";
        var registered = new EventSyntax(
            $"{name}Registered",
            [new PropertySyntax("reference", new TypeRefSyntax("String", false, false, SourceLocation.Start), SourceLocation.Start)],
            SourceLocation.Start);

        var from = new FromSyntax(
            [new EventSpecSyntax($"{name}Registered", null, SourceLocation.Start)],
            null,
            null,
            [new SetMappingSyntax("reference", new PathExpressionSyntax("reference", SourceLocation.Start), SourceLocation.Start)],
            SourceLocation.Start);

        var projection = new ProjectionSyntax(
            $"{name}Summary",
            $"{name}Summary",
            null,
            autoMap,
            new ExpressionKeySyntax(new PathExpressionSyntax("reference", SourceLocation.Start), SourceLocation.Start),
            [from],
            SourceLocation.Start);

        var slice = new SliceSyntax(
            SliceType.StateView, $"{name}Summary", [registered], [], [], [projection], [], [], [], [], [], SourceLocation.Start);

        var feature = new FeatureSyntax($"{name}Things", [], [slice], SourceLocation.Start);
        return new ApplicationSyntax([], [], [], [new ModuleSyntax($"{name}Module", [], [feature], SourceLocation.Start)], SourceLocation.Start);
    }
}
