// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Scene.Model.Layouts;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Scene.for_ArrangementConverter.when_converting_with_diagnostics;

public class and_a_size_is_invalid : Specification
{
    [Theory]
    [InlineData("bad", "regular", "regular", "arrangement size 'bad'")]
    [InlineData("NaN", "regular", "regular", "arrangement size 'NaN'")]
    [InlineData("1e999", "regular", "regular", "arrangement size '1e999'")]
    [InlineData("100", "unknown", "regular", "size class 'unknown'")]
    [InlineData("100", "regular", "unknown", "size class 'unknown'")]
    [InlineData("100", "99", "regular", "size class '99'")]
    public void should_report_the_size_and_omit_the_invalid_placement_or_variant(string size, string width, string height, string construct)
    {
        var location = new SourceLocation(10, 3, "shell.play");
        var syntax = new ArrangementSyntax(ArrangementMode.Freeform, location, Variants:
            [new VariantSyntax(width, height, [new PlaceSyntax("content", location, SizeWidth: size, SizeHeight: "fill")], location)]);
        var diagnostics = new ConversionDiagnostics();

        var result = (FreeformSlotArrangement)ArrangementConverter.Convert(syntax, diagnostics, "Shell")!;

        result.Variants.SelectMany(variant => variant.Placements).ShouldBeEmpty();
        diagnostics.All.Count.ShouldEqual(1);
        diagnostics.All[0].Code.ShouldEqual("STAGE-SCENE-ARRANGEMENT-004");
        diagnostics.All[0].Severity.ShouldEqual(DiagnosticSeverity.Error);
        diagnostics.All[0].Construct.ShouldEqual(construct);
        diagnostics.All[0].Location.ShouldEqual(location);
        diagnostics.All[0].Path.ShouldEqual("Shell");
    }
}
