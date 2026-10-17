// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Scene.Model.Layouts;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Scene.for_ArrangementConverter.when_converting_with_diagnostics;

public class and_a_subtree_is_unsupported : Specification
{
    [Theory]
    [InlineData(false, "STAGE-SCENE-ARRANGEMENT-001", "arrangement node UnknownNode")]
    [InlineData(true, "STAGE-SCENE-ARRANGEMENT-002", "arrangement container kind 99")]
    public void should_omit_only_the_unsupported_subtree(bool container, string code, string construct)
    {
        var location = new SourceLocation(8, 5, "layout.play");
        ArrangementNodeSyntax unsupported = container ? new ArrangementContainerSyntax((ArrangementContainerKind)99, [], location) : new UnknownNode(location);
        var syntax = new ArrangementSyntax(
            ArrangementMode.Flow,
            location,
            new ArrangementContainerSyntax(ArrangementContainerKind.Row, [unsupported, new ArrangementSlotSyntax("content", location)], location));
        var diagnostics = new ConversionDiagnostics();

        var result = (FlowArrangement)ArrangementConverter.Convert(syntax, diagnostics, "Shell")!;

        ((FlowContainer)result.Root).Children.Count.ShouldEqual(1);
        diagnostics.All.Count.ShouldEqual(1);
        diagnostics.All[0].Code.ShouldEqual(code);
        diagnostics.All[0].Construct.ShouldEqual(construct);
        diagnostics.All[0].Severity.ShouldEqual(DiagnosticSeverity.Error);
        diagnostics.All[0].Location.ShouldEqual(location);
        diagnostics.All[0].Path.ShouldEqual("Shell");
    }

    sealed record UnknownNode(SourceLocation Location) : ArrangementNodeSyntax(Location);
}
