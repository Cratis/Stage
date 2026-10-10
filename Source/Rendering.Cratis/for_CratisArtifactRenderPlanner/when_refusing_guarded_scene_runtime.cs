// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Scene;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_refusing_guarded_scene_runtime : Specification
{
    const string GuardedAction = """
                    action "Register"
                      when item.name == "Ready" execute RegisterProject
                      otherwise hidden
            """;

    const string GuardedInteraction = """
                    on click
                      when item.name == "Ready"
                        execute RegisterProject
                      otherwise
                        notify info "Not ready"
            """;

    [Theory]
    [InlineData("action")]
    [InlineData("interaction")]
    public void should_render_guards_the_runtime_evaluates(string kind)
    {
        var scene = Scene(kind == "action" ? GuardedAction : GuardedInteraction);
        var profile = CratisRendering.CreateProfile("Projects", new("Projects", "Projects"), scene);

        scene.RuntimeIssues.ShouldBeEmpty();
        profile.Inputs.Any(input => input.Name.EndsWith("scene.json", StringComparison.Ordinal)).ShouldBeTrue();
    }

    // Screenplay refuses an unevaluable guard while compiling, so the issue admission records for one is placed on
    // the Scene directly: whatever produced it, it is refused before any runnable input is created.
    [Theory]
    [InlineData("action", "STAGE-SCENE-ACTION-001")]
    [InlineData("interaction", "STAGE-SCENE-INTERACTION-001")]
    public void should_refuse_unevaluable_guards_before_creating_runnable_frontend_inputs(string kind, string code)
    {
        var scene = Scene(kind == "action" ? GuardedAction : GuardedInteraction) with
        {
            RuntimeIssues = [new(code, "Register", global::Cratis.Screenplay.Diagnostics.SourceLocation.Start, "unevaluable")]
        };
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
