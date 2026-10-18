// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Stage.Contracts.Scene;
using Cratis.Stage.Rendering.Cratis.Scene;
using SceneElements = Cratis.Scene.Model.Elements;
using SceneForms = Cratis.Scene.Model.Forms;
using SceneScreens = Cratis.Scene.Model.Screens;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;

internal static class escaped_member_application
{
    internal const string Source = """
        policy Owner
          require claim "owner" matches subject and claim "target" matches text
        module Work
          feature Processing
            slice Automation FollowUp
              command Text
                authorize Owner
                text String identifier
                handle String
                produces Completed
                  for text
                  value = handle
              event Started
                id String
                value String
              event Completed
                value String
              reaction Worker
                when Started
                  invokes Text
                    text = id
                    handle = value
              specification Completing
                when append Started
                  id = "one"
                  value = "payload"
                then denied
            slice StateView Lookup
              readmodel Details
                id String
                value String
              query Details => Details?
                by id String
              projection DetailsProjection => Details
                from Completed key $eventSourceId
                  id = $eventSourceId
                  value = value
        """;

    internal static SceneApplication Scene()
    {
        var layout = DefaultLayout.Create();
        var form = new SceneForms.Form(
            "TextForm",
            "Text",
            null,
            [new("text", null, null, "Identity", null), new("handle", "selection.value", null, "Value", null)],
            SceneForms.FormGenerationMode.Manual,
            new SceneForms.CommandFormLayout([], [new("handle", 1, 1, null, 1, null)], null, null));
        var action = new SceneElements.ExternalComponent
        {
            Id = "submit", Name = "submit", ComponentName = "core:action",
            Properties = new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["command"] = "Text",
                ["arguments"] = new Dictionary<string, object?> { ["handle"] = "selection.value" }
            }
        };
        var toolbar = new SceneElements.ExternalComponent
        {
            Id = "toolbar", Name = "toolbar", ComponentName = "core:toolbar",
            Properties = new Dictionary<string, object?>(StringComparer.Ordinal),
            Slots = new Dictionary<string, IReadOnlyList<SceneElements.SceneElement>>(StringComparer.Ordinal)
            {
                ["items"] = [new SceneElements.ExternalComponent
                {
                    Id = "complete", Name = "complete", ComponentName = "core:toolbarButton",
                    Properties = new Dictionary<string, object?>(StringComparer.Ordinal) { ["slice"] = "FollowUp" }
                }]
            }
        };
        var screen = new SceneScreens.Screen(
            "Work",
            layout.Name,
            new Dictionary<string, IReadOnlyList<SceneElements.SceneElement>>(StringComparer.Ordinal)
            {
                [DefaultLayout.ContentSlotName] = [action, toolbar]
            },
            [form],
            []);
        return new([], [], [layout], [], [], [screen]);
    }
}
