// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax.Projections;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.Renderers;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_StateViewSliceRenderer;

public class when_preflighting_unrenderable_projection_blocks : Specification
{
    [Theory]
    [InlineData("all\n  count total")]
    [InlineData("children lines identified by id\n  join labels on id\n    with LabelChanged\n      name = name")]
    [InlineData("children lines identified by id\n  every\n    name = name")]
    [InlineData("children lines identified by id\n  remove via join on ItemRemoved")]
    [InlineData("children lines identified by id\n  clear with ItemRemoved")]
    [InlineData("nested details\n  from ItemRegistered\n    name = name\n  every\n    name = name")]
    [InlineData("nested details\n  from ItemRegistered\n    name = name\n  remove with ItemRemoved")]
    [InlineData("children lines identified by id\n  from ItemRegistered key id\n    id = id\n  remove with ItemRemoved key missing")]
    [InlineData("from ItemRegistered\n  clear name")]
    [InlineData("from ItemRegistered\n  name = $causedBy.name")]
    public void should_reject_before_emitting_any_read_model_source(string blocks)
    {
        var source = "module Catalog\n  feature Items\n    slice StateView Summary\n      projection Summary => SummaryModel\n" +
            string.Join('\n', blocks.Split('\n').Select(_ => $"        {_}"));
        var compilation = new ScreenplayCompiler().Compile(source);
        Assert.True(compilation.Success, string.Join(Environment.NewLine, compilation.Diagnostics));
        var applicationSet = new ApplicationSet([compilation.Value!]);
        var error = Catch.Exception(() => new StateViewSliceRenderer().Render(applicationSet.Slices.Single(), applicationSet, "CratisApp"));
        Assert.IsType<UnsupportedLegacyProjection>(error);
        Assert.Contains(UnsupportedLegacyProjection.DiagnosticCode, error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void should_reject_an_unknown_block_kind()
    {
        const string source = """
            module Catalog
              feature Items
                slice StateView Summary
                  projection Summary => SummaryModel
                    from ItemRegistered
                      name = name
            """;
        var compilation = new ScreenplayCompiler().Compile(source);
        Assert.True(compilation.Success);
        var application = compilation.Value!;
        var module = application.Modules.Single();
        var feature = module.Features.Single();
        var slice = feature.Slices.Single();
        var projection = slice.Projections.Single();
        var changed = application with { Modules = [module with { Features = [feature with
        {
            Slices = [slice with { Projections = [projection with { Blocks = [.. projection.Blocks, new UnknownBlock(SourceLocation.Start)] }] }]
        }] }] };
        var set = new ApplicationSet([changed]);
        Assert.IsType<UnsupportedLegacyProjection>(Catch.Exception(() => new StateViewSliceRenderer().Render(set.Slices.Single(), set, "CratisApp")));
    }

    sealed record UnknownBlock(SourceLocation Location) : ProjectionBlockSyntax(Location);
}
