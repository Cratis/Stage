// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Scene.Model.Elements;
using Cratis.Screenplay;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Scene.for_SceneRuntimeAdmission;

public class when_translating_guarded_constructs : Specification
{
    SceneApplication _scene = null!;

    void Because()
    {
        const string source = """
            behavior RegisterWhenReady
              on click
                when item.name == "Ready"
                  execute RegisterProject
                otherwise
                  notify info "Not ready"
            module Projects
              uses RegisterWhenReady
              feature Registration
                on double click
                  when item.name == "Ready"
                    execute RegisterProject
                slice StateChange Registration
                  command RegisterProject
                  screen Overview
                    section "Registration"
                      action "Register"
                        when item.name == "Ready" execute RegisterProject
                        otherwise hidden
            """;
        var compilation = new ScreenplayCompiler().Compile(source);
        Assert.True(compilation.Success, string.Join(Environment.NewLine, compilation.Diagnostics));
        _scene = new ScreenplaySceneVisitor().Visit(compilation.Value!);
    }

    [Fact] void should_collect_nested_guarded_actions() => _scene.RuntimeIssues.Count(issue => issue.Code == UnsupportedGuardedScreenAction.DiagnosticCode).ShouldEqual(1);
    [Fact] void should_collect_named_and_inherited_guarded_interactions() => _scene.RuntimeIssues.Count(issue => issue.Code == UnsupportedGuardedInteraction.DiagnosticCode).ShouldEqual(2);
    [Fact] void should_preserve_guarded_action_contract_output() => ((ExternalComponent)_scene.Screens.Single().SlotContent[DefaultLayout.ContentSlotName].Single()).Slots["content"].Single().Properties.ContainsKey("alternatives").ShouldBeTrue();
    [Fact] void should_keep_runtime_issues_out_of_the_scene_wire_contract() => JsonSerializer.Serialize(_scene).ShouldNotContain("RuntimeIssues");
}
