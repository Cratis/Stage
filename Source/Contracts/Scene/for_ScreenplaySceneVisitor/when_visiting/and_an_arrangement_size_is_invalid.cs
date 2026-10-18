// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Scene.for_ScreenplaySceneVisitor.when_visiting;

public class and_an_arrangement_size_is_invalid : Specification
{
    [Theory]
    [InlineData("unknown", "regular", "100", typeof(UnknownSizeClassName))]
    [InlineData("regular", "unknown", "100", typeof(UnknownSizeClassName))]
    [InlineData("regular", "regular", "invalid", typeof(FormatException))]
    public void should_preserve_the_legacy_exception_while_conversion_collects_the_diagnostic(string width, string height, string size, Type exceptionType)
    {
        var location = new SourceLocation(10, 3, "shell.play");
        var arrangement = new ArrangementSyntax(ArrangementMode.Freeform, location, Variants:
            [new VariantSyntax(width, height, [new PlaceSyntax("content", location, SizeWidth: size, SizeHeight: "fill")], location)]);
        var layout = new LayoutSyntax("Shell", [new SlotSyntax("content", null, location)], location, arrangement);
        var application = new ApplicationSyntax([], [], [], [], location) { Layouts = [layout] };

        var legacyError = Catch.Exception(() => ArrangementConverter.Convert(arrangement));
        var error = Catch.Exception(() => new ScreenplaySceneVisitor().Visit(application));
        var collected = new ScreenplaySceneVisitor().Convert(application);

        error.GetType().ShouldEqual(exceptionType);
        error.Message.ShouldEqual(legacyError.Message);
        collected.ConversionDiagnostics.Single().Code.ShouldEqual("STAGE-SCENE-ARRANGEMENT-004");
        collected.Layouts.Single().Name.ShouldEqual("Shell");
    }
}
