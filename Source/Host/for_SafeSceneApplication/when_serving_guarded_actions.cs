// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Scene.Model.Elements;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Scene;
using Xunit;
using SceneScreens = Cratis.Scene.Model.Screens;

namespace Cratis.Stage.Host.for_SafeSceneApplication;

/// <summary>
/// The Scene a running Stage serves keeps every guarded action its runtime evaluates and leaves out any it cannot.
/// </summary>
public class when_serving_guarded_actions : Specification
{
    static readonly Dictionary<string, object?> _evaluable = new()
    {
        ["kind"] = "comparison",
        ["left"] = new Cratis.Scene.Model.Common.BindingExpression("item.status"),
        ["operator"] = "Equal",
        ["right"] = "open",
    };

    SceneApplication _served = null!;

    void Because()
    {
        var content = new Dictionary<string, IReadOnlyList<SceneElement>>
        {
            ["content"] =
            [
                Action("evaluable", _evaluable, new Dictionary<string, object?> { ["outcome"] = "Hidden" }),
                Action("against-a-path", new Dictionary<string, object?> { ["kind"] = "ComparisonConditionSyntax" }, null),
                Action("unknown-fallback", _evaluable, new Dictionary<string, object?> { ["outcome"] = "Unknown" }),
                SceneElementFactory.Component("unguarded", "core:action", new Dictionary<string, object?> { ["command"] = "RenameWorkItem" }),
            ]
        };
        var screen = new SceneScreens.Screen("Details", "Default", content, [], []);
        _served = SafeSceneApplication.From(new SceneApplication([], [], [], [], [], [screen]));
    }

    [Fact] void should_serve_the_evaluable_guarded_action() => Ids().ShouldContain("evaluable");
    [Fact] void should_leave_out_a_guard_it_cannot_evaluate() => Ids().ShouldNotContain("against-a-path");
    [Fact] void should_leave_out_an_unknown_fallback() => Ids().ShouldNotContain("unknown-fallback");
    [Fact] void should_serve_unguarded_actions() => Ids().ShouldContain("unguarded");

    static ExternalComponent Action(string id, Dictionary<string, object?> condition, Dictionary<string, object?>? otherwise)
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

        return SceneElementFactory.Component(id, "core:action", properties);
    }

    string[] Ids() => [.. _served.Screens.Single().SlotContent["content"].Select(_ => _.Id)];
}
