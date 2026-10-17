// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Scene.for_ScreenplaySceneVisitor.when_visiting;

public class and_an_arrangement_node_is_unknown : Specification
{
    Exception? _error;
    SceneApplication _collected = null!;

    void Because()
    {
        var location = new SourceLocation(8, 5, "shell.play");
        var arrangement = new ArrangementSyntax(ArrangementMode.Flow, location, new UnknownNode(location));
        var layout = new LayoutSyntax("Shell", [new SlotSyntax("content", null, location)], location, arrangement);
        var application = new ApplicationSyntax([], [], [], [], location) { Layouts = [layout] };
        _error = Catch.Exception(() => new ScreenplaySceneVisitor().Visit(application));
        _collected = new ScreenplaySceneVisitor().Convert(application);
    }

    [Fact] void should_preserve_the_legacy_refusal() => _error.ShouldBeOfExactType<InvalidEventModel>();
    [Fact] void should_name_the_diagnostic() => _error!.Message.ShouldContain("STAGE-SCENE-ARRANGEMENT-001");
    [Fact] void should_name_the_owning_structure() => _error!.Message.ShouldContain("Shell");
    [Fact] void should_include_the_source_location() => _error!.Message.ShouldContain("shell.play(8,5)");
    [Fact] void should_let_collecting_callers_retain_the_diagnostic() => _collected.ConversionDiagnostics.Single().Code.ShouldEqual("STAGE-SCENE-ARRANGEMENT-001");
    [Fact] void should_keep_the_layout_in_the_collecting_result() => _collected.Layouts.Single().Name.ShouldEqual("Shell");

    sealed record UnknownNode(SourceLocation Location) : ArrangementNodeSyntax(Location);
}
