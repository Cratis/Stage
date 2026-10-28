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
    static readonly JsonSerializerOptions _camelCase = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

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

    [Fact] void should_admit_guarded_actions_the_runtime_evaluates() => _scene.RuntimeIssues.Any(issue => issue.Code == UnsupportedGuardedScreenAction.DiagnosticCode).ShouldBeFalse();
    [Fact] void should_admit_named_and_inherited_guarded_interactions_the_runtime_evaluates() => _scene.RuntimeIssues.Any(issue => issue.Code == UnsupportedGuardedInteraction.DiagnosticCode).ShouldBeFalse();
    [Fact] void should_carry_each_interaction_branch_as_its_own_guarded_binding() => GuardedBindings().Select(Kind).ShouldContain("firstMatch", "otherwise");
    [Fact] void should_carry_the_authored_guard_on_every_branch() => GuardedBindings().All(_ => _.ToJsonString().Contains("\"right\":\"Ready\"", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_preserve_guarded_action_contract_output() => ((ExternalComponent)_scene.Screens.Single().SlotContent[DefaultLayout.ContentSlotName].Single()).Slots["content"].Single().Properties.ContainsKey("alternatives").ShouldBeTrue();

    // Behaviors attach at every level - the module, the feature, the screen and its elements - so read them all.
    IEnumerable<System.Text.Json.Nodes.JsonObject> GuardedBindings() =>
        Nodes(JsonSerializer.SerializeToNode(_scene, _camelCase))
            .Where(_ => _["condition"]?["path"]?.GetValue<string>() == SceneGuards.GuardPath);

    static IEnumerable<System.Text.Json.Nodes.JsonObject> Nodes(System.Text.Json.Nodes.JsonNode? node) => node switch
    {
        System.Text.Json.Nodes.JsonObject @object => new[] { @object }.Concat(@object.SelectMany(_ => Nodes(_.Value))),
        System.Text.Json.Nodes.JsonArray array => array.SelectMany(Nodes),
        _ => []
    };

    static string? Kind(System.Text.Json.Nodes.JsonObject binding) => binding["condition"]?["value"]?["kind"]?.GetValue<string>();

    [Fact] void should_keep_runtime_issues_out_of_the_scene_wire_contract() => JsonSerializer.Serialize(_scene).ShouldNotContain("RuntimeIssues");
}
