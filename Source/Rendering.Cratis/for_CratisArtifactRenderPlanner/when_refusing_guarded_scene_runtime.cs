// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Scene;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_refusing_guarded_scene_runtime : Specification
{
    [Theory]
    [InlineData("action", "STAGE-SCENE-ACTION-001")]
    [InlineData("interaction", "STAGE-SCENE-INTERACTION-001")]
    public void should_refuse_before_creating_runnable_frontend_inputs(string kind, string code)
    {
        var directive = kind == "action" ? """
                    action "Register"
                      when item.name == "Ready" execute RegisterProject
                      otherwise hidden
            """ : """
                    on click
                      when item.name == "Ready"
                        execute RegisterProject
                      otherwise
                        notify info "Not ready"
            """;
        var scene = Scene(directive);
        var error = Catch.Exception(() => CratisRendering.CreateProfile("Projects", new("Projects", "Projects"), scene));
        error.ShouldNotBeNull();
        error.Message.ShouldContain(code);
        error.Message.ShouldContain("https://github.com/Cratis/Scene/issues/68");
        error.Message.ShouldContain("https://github.com/Cratis/Stage/issues/209");
    }

    [Fact]
    public void should_keep_unguarded_actions_renderable()
    {
        var scene = Scene("        action RegisterProject");
        var profile = CratisRendering.CreateProfile("Projects", new("Projects", "Projects"), scene);
        profile.Inputs.Any(input => input.Name.EndsWith("scene.json", StringComparison.Ordinal)).ShouldBeTrue();
        scene.RuntimeIssues.ShouldBeEmpty();
    }

    static SceneApplication Scene(string directive)
    {
        var source = """
            module Projects
              feature Registration
                slice StateChange RegisterProject
                  command RegisterProject
                  screen Overview
            """ + "\n" + directive;
        var compilation = new ScreenplayCompiler().Compile(source);
        Assert.True(compilation.Success, string.Join(Environment.NewLine, compilation.Diagnostics));

        return new ScreenplaySceneVisitor().Visit(compilation.Value!);
    }
}
