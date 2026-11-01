// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Scene.Model.Elements;
using Cratis.Scene.Model.Interactions;
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
    [Fact]
    void should_preserve_the_named_behaviors_authored_otherwise()
    {
        var bindings = BindingsFor(InteractionTriggerKind.Click).ToArray();
        Assert.NotEmpty(bindings);
        foreach (var binding in bindings)
        {
            ((System.Text.Json.Nodes.JsonArray)binding["alternatives"]!).Count.ShouldEqual(1);
            ((System.Text.Json.Nodes.JsonArray)binding["otherwise"]!).Count.ShouldEqual(1);
            binding["otherwise"]!.ToJsonString().ShouldContain("Not ready");
        }
    }

    [Fact]
    void should_not_invent_an_otherwise_for_the_feature_local_binding()
    {
        var bindings = BindingsFor(InteractionTriggerKind.DoubleClick).ToArray();
        Assert.NotEmpty(bindings);
        foreach (var binding in bindings)
        {
            ((System.Text.Json.Nodes.JsonArray)binding["alternatives"]!).Count.ShouldEqual(1);
            (binding["otherwise"] is null or System.Text.Json.Nodes.JsonArray { Count: 0 }).ShouldBeTrue();
        }
    }

    [Fact]
    void should_carry_the_authored_guard_on_every_branch()
    {
        var bindings = GuardedBindings().ToArray();
        Assert.NotEmpty(bindings);
        bindings.All(binding => binding["alternatives"]!.ToJsonString().Contains("\"right\":\"Ready\"", StringComparison.Ordinal)).ShouldBeTrue();
        bindings.All(binding => binding["actions"] is System.Text.Json.Nodes.JsonArray { Count: 0 }).ShouldBeTrue();
    }

    IEnumerable<System.Text.Json.Nodes.JsonObject> BindingsFor(InteractionTriggerKind kind) => GuardedBindings()
        .Where(binding => binding["trigger"]!["kind"]!.GetValue<int>() == (int)kind);

    [Fact] void should_preserve_guarded_action_contract_output() => ((ExternalComponent)_scene.Screens.Single().SlotContent[DefaultLayout.ContentSlotName].Single()).Slots["content"].Single().Properties.ContainsKey("alternatives").ShouldBeTrue();

    // Behaviors attach at every level - the module, the feature, the screen and its elements - so read them all.
    IEnumerable<System.Text.Json.Nodes.JsonObject> GuardedBindings() =>
        Nodes(JsonSerializer.SerializeToNode(_scene, _camelCase))
            .Where(_ => _["trigger"] is not null && _["alternatives"] is System.Text.Json.Nodes.JsonArray);

    static IEnumerable<System.Text.Json.Nodes.JsonObject> Nodes(System.Text.Json.Nodes.JsonNode? node) => node switch
    {
        System.Text.Json.Nodes.JsonObject @object => new[] { @object }.Concat(@object.SelectMany(_ => Nodes(_.Value))),
        System.Text.Json.Nodes.JsonArray array => array.SelectMany(Nodes),
        _ => []
    };

    [Fact] void should_keep_runtime_issues_out_of_the_scene_wire_contract() => JsonSerializer.Serialize(_scene).ShouldNotContain("RuntimeIssues");
}
