// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Scene.Model.Elements;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Scene;
using Cratis.Stage.Host.for_StageHttpSurface.given;
using Xunit;
using SceneScreens = Cratis.Scene.Model.Screens;

namespace Cratis.Stage.Host.for_SafeSceneApplication;

/// <summary>
/// The Scene a running Stage serves keeps every guarded action its runtime evaluates and leaves out any it cannot.
/// </summary>
public class when_serving_guarded_actions : for_StageEndpointMapper.given.a_routed_model
{
    static readonly Dictionary<string, object?> _evaluable = new()
    {
        ["kind"] = "comparison",
        ["left"] = new Cratis.Scene.Model.Guards.SubjectReference("item.status"),
        ["operator"] = "Equal",
        ["right"] = "open",
    };

    SceneApplication _served = null!;

    void Because()
    {
        MapModel(RouteModels.Model(RouteModels.Command("Close", "CloseWorkItem")));
        var content = new Dictionary<string, IReadOnlyList<SceneElement>>
        {
            ["content"] =
            [
                Action("evaluable", _evaluable, new Dictionary<string, object?> { ["outcome"] = "Hidden" }),
                Action("legacy-evaluable", _evaluable, new Dictionary<string, object?> { ["outcome"] = "Hidden" }, "core:action"),
                Action("legacy-envelope", new Dictionary<string, object?> { ["path"] = "$guard", ["parameters"] = new { predicate = _evaluable } }, null),
                Action("legacy-unsupported", new Dictionary<string, object?> { ["kind"] = "ComparisonConditionSyntax" }, null, "core:action"),
                Action("against-a-path", new Dictionary<string, object?> { ["kind"] = "ComparisonConditionSyntax" }, null),
                Action("unknown-fallback", _evaluable, new Dictionary<string, object?> { ["outcome"] = "Unknown" }),
                SceneElementFactory.Component("native-without-branches", "core:guarded-action", new Dictionary<string, object?> { ["label"] = "Close" }),
                SceneElementFactory.Component("unguarded", "core:action", new Dictionary<string, object?> { ["command"] = "RenameWorkItem" }),
            ]
        };
        var screen = new SceneScreens.Screen("Details", "Default", content, [], []);
        _served = SafeSceneApplication.From(new SceneApplication([], [], [], [], [], [screen]));
    }

    [Fact] void should_serve_the_evaluable_guarded_action() => Ids().ShouldContain("evaluable");
    [Fact] void should_leave_out_a_guard_it_cannot_evaluate() => Ids().ShouldNotContain("against-a-path");
    [Fact] void should_leave_out_an_unknown_fallback() => Ids().ShouldNotContain("unknown-fallback");
    [Fact] void should_leave_out_a_native_action_without_branches() => Ids().ShouldNotContain("native-without-branches");
    [Fact] void should_serve_unguarded_actions() => Ids().ShouldContain("unguarded");
    [Fact] void should_preserve_the_native_guarded_component() => ((ExternalComponent)_served.Screens.Single().SlotContent["content"].Single(element => element.Id == "evaluable")).ComponentName.ShouldEqual("core:guarded-action");
    [Fact] void should_preserve_supported_legacy_actions() => Ids().ShouldContain("legacy-evaluable");
    [Fact] void should_refuse_legacy_guard_envelopes() => Ids().ShouldNotContain("legacy-envelope");
    [Fact] void should_refuse_unsupported_legacy_predicates() => Ids().ShouldNotContain("legacy-unsupported");
    [Fact] void should_serve_exactly_the_supported_actions() => Ids().ShouldEqual(["evaluable", "legacy-evaluable", "unguarded"]);
    [Fact] void should_not_execute_commands_while_loading_the_scene() => _commands.ShouldBeEmpty();
    [Fact] void should_not_append_events_while_loading_the_scene() => _appends.ShouldBeEmpty();

    [Fact]
    async Task should_observe_dispatch_when_the_command_is_actually_requested()
    {
        _commands.ShouldBeEmpty();
        var response = await Request("POST", "/api/orders/checkout/close/close-work-item", """{"orderId":"11111111-1111-1111-1111-111111111111"}""");
        response.Status.ShouldEqual(200);
        _commands.Count.ShouldEqual(1);
        _appends.Count.ShouldEqual(1);
    }

    static ExternalComponent Action(string id, Dictionary<string, object?> condition, Dictionary<string, object?>? otherwise, string componentName = "core:guarded-action")
    {
        var properties = new Dictionary<string, object?>
        {
            ["label"] = "Close",
            ["alternatives"] = new List<Dictionary<string, object?>> { new() { ["command"] = "CloseWorkItem", ["condition"] = condition, ["arguments"] = new Dictionary<string, object?>() } },
        };
        if (otherwise is not null)
        {
            properties["otherwise"] = otherwise;
        }

        return SceneElementFactory.Component(id, componentName, properties);
    }

    string[] Ids() => [.. _served.Screens.Single().SlotContent["content"].Select(_ => _.Id)];
}
