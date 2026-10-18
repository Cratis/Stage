// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Scene.for_ScreenplaySceneVisitor.when_converting;

public class and_template_metadata_is_unsupported : Specification
{
    [Theory]
    [InlineData("layout")]
    [InlineData("screen")]
    [InlineData("dialog")]
    public void should_thread_the_collector_through_every_template_kind(string kind)
    {
        var location = new SourceLocation(6, 3, "templates.play");
        var layout = new LayoutSyntax("Shell", [], location) { RestrictsScopes = true, Scopes = kind == "layout" ? ["unknown", "application"] : ["application"] };
        var screen = new ScreenTemplateSyntax("Workspace", [], location) { RestrictsScopes = true, Scopes = kind == "screen" ? ["unknown", "module"] : ["module"] };
        var dialog = new DialogTemplateSyntax("Help", [], location) { RestrictsScopes = true, Scopes = kind == "dialog" ? ["unknown", "feature"] : ["feature"] };
        var module = new ModuleSyntax("Projects", [], [], location) { ScreenTemplates = [screen], DialogTemplates = [dialog] };
        var syntax = new ApplicationSyntax([], [], [], [module], location) { Layouts = [layout] };

        var result = new ScreenplaySceneVisitor().Convert(syntax);

        result.Layouts.Count.ShouldEqual(1);
        result.ScreenTemplates.Count.ShouldEqual(1);
        result.DialogTemplates.Count.ShouldEqual(1);
        result.ConversionDiagnostics.Single().Code.ShouldEqual("STAGE-SCENE-COMPOSITION-002");
        result.ConversionDiagnostics.Single().Location.ShouldEqual(location);
        Catch.Exception(() => new ScreenplaySceneVisitor().Visit(syntax)).ShouldBeOfExactType<UnknownCompositionValue>();
    }
}
