// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Scene.for_ScreenplaySceneVisitor.when_converting;

public class and_a_layout_arrangement_cannot_convert : Specification
{
    SceneApplication _result = null!;

    void Because()
    {
        var location = new SourceLocation(4, 5, "shell.play");
        var arrangement = new ArrangementSyntax(ArrangementMode.Flow, location, new ArrangementContainerSyntax((ArrangementContainerKind)99, [], location));
        var layout = new LayoutSyntax("Shell", [new SlotSyntax("content", null, location)], location, arrangement);
        var application = new ApplicationSyntax([], [], [], [], location) { Layouts = [layout, layout with { Name = "Other", Arrangement = null }] };
        _result = new ScreenplaySceneVisitor().Convert(application);
    }

    [Fact] void should_keep_every_layout() => _result.Layouts.Count.ShouldEqual(2);
    [Fact] void should_keep_the_unsupported_layouts_slots() => _result.Layouts[0].Slots.Count.ShouldEqual(1);
    [Fact] void should_omit_the_unsupported_arrangement() => _result.Layouts[0].Arrangement.ShouldBeNull();
    [Fact] void should_report_the_construct() => _result.ConversionDiagnostics.Single().Code.ShouldEqual("STAGE-SCENE-ARRANGEMENT-002");
    [Fact] void should_report_the_source_location() => _result.ConversionDiagnostics.Single().Location.ShouldEqual(new SourceLocation(4, 5, "shell.play"));
    [Fact] void should_keep_the_diagnostics_for_runtime_admission() => _result.ForRuntime().ConversionDiagnostics.Count.ShouldEqual(1);
}
