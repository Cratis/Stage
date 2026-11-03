// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Scene.Model.Common;
using Cratis.Scene.Model.Interactions;

namespace Cratis.Stage.Contracts.Scene;

/// <summary>
/// Maps only explicitly response-sourced form bindings to their transport member names.
/// </summary>
public static class ResponseMemberNames
{
    /// <summary>
    /// Maps response member names without changing data, route, literal or state bindings.
    /// </summary>
    /// <param name="scene">The translated scene.</param>
    /// <param name="member">The transport name of a response field.</param>
    /// <returns>The same scene with its form response paths mapped.</returns>
    public static SceneApplication Apply(SceneApplication scene, Func<string, string> member) => scene with
    {
        Screens = [.. scene.Screens.Select(screen => screen with
        {
            Forms = [.. screen.Forms.Select(form => form with { OnSubmit = [.. form.OnSubmit.Select(action => Action(action, member))] })]
        })]
    };

    /// <summary>
    /// Maps explicit response paths in a serialized runtime scene, in place.
    /// </summary>
    /// <param name="node">The runtime document.</param>
    /// <param name="member">The transport name of a response field.</param>
    public static void Apply(JsonNode? node, Func<string, string> member)
    {
        switch (node)
        {
            case JsonObject value:
                ApplyObject(value, member);
                break;
            case JsonArray array:
                ApplyArray(array, member);
                break;
        }
    }

    static void ApplyObject(JsonObject value, Func<string, string> member)
    {
        Rewrite(value, member);
        foreach (var (_, child) in value) Apply(child, member);
    }

    static void ApplyArray(JsonArray array, Func<string, string> member)
    {
        foreach (var child in array) Apply(child, member);
    }

    static void Rewrite(JsonObject value, Func<string, string> member)
    {
        if (ReadString(value["kind"]) != "commandResponse") return;
        var path = ReadString(value["path"]);
        if (path is not { Length: > 0 }) return;
        value["path"] = member(path);
    }

    static string? ReadString(JsonNode? node)
    {
        if (node is not JsonValue value) return null;
        return value.TryGetValue<string>(out var text) ? text : null;
    }

    static BindingExpression Value(BindingExpression value, Func<string, string> member) =>
        value.Kind == BindingSourceKind.CommandResponse && value.Path.Length > 0 ? value with { Path = member(value.Path) } : value;

    static InteractionAction Action(InteractionAction action, Func<string, string> member) => Operand(action, member) with
    {
        Arguments = [.. action.Arguments.Select(argument => argument with { Value = Value(argument.Value, member) })],
        OnSuccess = [.. action.OnSuccess.Select(child => Action(child, member))],
        OnFailure = [.. action.OnFailure.Select(child => Action(child, member))],
        OnResult = [.. action.OnResult.Select(child => Action(child, member))]
    };

    static InteractionAction Operand(InteractionAction action, Func<string, string> member) => action switch
    {
        SetStateAction state => state with { Value = Value(state.Value, member) },
        NotifyAction notify => notify with { Message = Message(notify.Message, member) },
        ConfirmAction confirm => confirm with { Message = Message(confirm.Message, member) },
        _ => action
    };

    static InteractionMessage Message(InteractionMessage message, Func<string, string> member) => message.Binding is { } binding ? message with { Binding = Value(binding, member) } : message;
}
