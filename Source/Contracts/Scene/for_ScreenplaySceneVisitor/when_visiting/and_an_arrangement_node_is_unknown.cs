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
    Exception? _legacyError;

    void Because()
    {
        var location = new SourceLocation(8, 5, "shell.play");
        var arrangement = new ArrangementSyntax(ArrangementMode.Flow, location, new UnknownNode(location));
        var layout = new LayoutSyntax("Shell", [new SlotSyntax("content", null, location)], location, arrangement);
        var laterArrangement = arrangement with { Root = new ArrangementContainerSyntax((ArrangementContainerKind)999, [], location) };
        var application = new ApplicationSyntax([], [], [], [], location) { Layouts = [layout, layout with { Name = "Later", Arrangement = laterArrangement }] };
        _legacyError = Catch.Exception(() => ArrangementConverter.Convert(arrangement));
        _error = Catch.Exception(() => new ScreenplaySceneVisitor().Visit(application));
        _collected = new ScreenplaySceneVisitor().Convert(application);
    }

    [Fact] void should_preserve_the_first_legacy_refusal() => _error.ShouldBeOfExactType<UnknownArrangementNode>();
    [Fact] void should_preserve_the_legacy_message() => _error!.Message.ShouldEqual(_legacyError!.Message);
    [Fact] void should_let_collecting_callers_retain_the_diagnostic() => _collected.ConversionDiagnostics[0].Code.ShouldEqual("STAGE-SCENE-ARRANGEMENT-001");
    [Fact] void should_collect_the_later_error() => _collected.ConversionDiagnostics[^1].Code.ShouldEqual("STAGE-SCENE-ARRANGEMENT-002");
    [Fact] void should_keep_the_layout_in_the_collecting_result() => _collected.Layouts[0].Name.ShouldEqual("Shell");

    sealed record UnknownNode(SourceLocation Location) : ArrangementNodeSyntax(Location);
}
