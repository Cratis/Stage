// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Contracts.Scene;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_composing_command_names.given;
using Cratis.Stage.Rendering.Cratis.Scene;
using Cratis.Stage.Rendering.Cratis.Semantics;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_GeneratedReactApplication.given;

/// <summary>
/// Renders two real slices with independently compiled primary and secondary commands sharing their names.
/// </summary>
public class a_secondary_toolbar_render : a_two_module_request
{
    /// <summary>
    /// Plans the generated browser application with all four command targets on its first screen.
    /// </summary>
    /// <returns>The generated application plan.</returns>
    protected ArtifactRenderPlan PlanApplication()
    {
        AddSecondSharedCommand(withSelectableQueries: true);
        _options = new("Workspaces", "Workspaces");
        CarryAuthoredScene(true);
        var context = new SemanticApplicationContext(_request, _options);
        Assert.Contains(context.Queries.Values, query => query.Name == "AllProjects");
        var input = _request.Profile.Inputs.Single(input => CratisArtifactRenderInput.TryCreateArtifact(input, out var artifact) && artifact!.RelativePath == SceneCompositionInput.RelativePath);
        var document = JsonNode.Parse(input.Bytes.AsSpan())!.AsObject();
        var content = document["screens"]![0]!["slotContent"]![DefaultLayout.ContentSlotName]!.AsArray();
        foreach (var entry in context.SceneCommands.ById.Values.OrderBy(entry => entry.Key, StringComparer.Ordinal))
        {
            var label = $"{(entry.Slice.Module.Name.StartsWith("Other", StringComparison.Ordinal) ? "B" : "A")} {entry.Command.Name}";
            var parameters = new Dictionary<string, object?>
            {
                [context.MemberNames.ProxyProperty(entry.Command.Properties[0])] = new { path = "projectId" },
                ["intruder"] = new { path = "projectId" }
            };
            content.Insert(0, CanonicalSceneJson.ToNode(new global::Cratis.Scene.Model.Elements.ExternalComponent
            {
                Id = label.Replace(' ', '-'),
                Name = label,
                ComponentName = "core:toolbarItem",
                Properties = new Dictionary<string, object?>
                {
                    ["name"] = label,
                    ["label"] = label,
                    ["kind"] = "Action",
                    ["target"] = entry.Key,
                    ["parameters"] = parameters
                }
            }));
        }

        // The authored composition has no schemas; the runtime renderer adds them under allocated command keys as JSON strings.
        var runtimeDocument = JsonNode.Parse(StageSceneDocument.Render(CanonicalSceneJson.Write(document), context))!.AsObject();
        var schemas = runtimeDocument["commandSchemas"]!.AsObject();
        var secondary = context.SceneCommands.ById.Values.Single(entry => entry.Slice.Module.Name.StartsWith("Other", StringComparison.Ordinal) && entry.Command.Name == "ArchiveProject");
        AddInlineForm(content, secondary, schemas[secondary.Key]!.GetValue<string>());
        var primary = context.SceneCommands.ById.Values.Single(entry => entry.Slice.Module.Name == "Projects" && entry.Command.Name == "ArchiveProject");
        AddOmittedMemberForm(content, primary, schemas[primary.Key]!.GetValue<string>());
        AddSelectableTable(content);
        AddFallbackScreen(document, secondary);
        var composed = CratisArtifactRenderInput.CreateText(SceneCompositionInput.RelativePath, CratisRendering.TargetVersion, CanonicalSceneJson.Write(document));
        var profile = _request.Profile;
        var request = _request with
        {
            Profile = ArtifactRenderProfile.Create(profile.Target, profile.TargetVersion, profile.Renderer, profile.RendererVersion, [.. profile.Inputs.Where(candidate => candidate.Name != composed.Name), composed])
        };
        var plan = _planner.Plan(request);
        Assert.True(plan.Success, string.Join(Environment.NewLine, plan.Diagnostics.Select(diagnostic => $"{diagnostic.Code}: {diagnostic.Message}")));
        return plan;
    }

    static void AddInlineForm(JsonArray content, SceneCommandEntry secondary, string schema)
    {
        content.Add(CanonicalSceneJson.ToNode(new global::Cratis.Scene.Model.Elements.ExternalComponent
        {
            Id = "inline-secondary-command",
            Name = "Inline secondary command",
            ComponentName = "Stage:commandForm",
            Properties = new Dictionary<string, object?>
            {
                ["command"] = secondary.Key,
                ["schema"] = schema,
                ["label"] = "Inline secondary",
                ["submitLabel"] = "Execute inline secondary",
                ["generationMode"] = "manual",
                ["fields"] = Array.Empty<object>(),
                ["arguments"] = new[]
                {
                    new { name = "projectId", source = "projectId" },
                    new { name = "archiveReason", source = "name" },
                    new { name = "intruder", source = "projectId" }
                }
            }
        }));
    }

    static void AddOmittedMemberForm(JsonArray content, SceneCommandEntry command, string schema)
    {
        content.Add(CanonicalSceneJson.ToNode(new global::Cratis.Scene.Model.Elements.ExternalComponent
        {
            Id = "native-omission-command",
            Name = "Native omission",
            ComponentName = "Stage:commandForm",
            Properties = new Dictionary<string, object?>
            {
                ["command"] = command.Key,
                ["schema"] = schema,
                ["label"] = "Native omission",
                ["submitLabel"] = "Execute native omission",
                ["generationMode"] = "manual",
                ["fields"] = Array.Empty<object>()
            }
        }));
    }

    static void AddSelectableTable(JsonArray content)
    {
        content.Add(CanonicalSceneJson.ToNode(new global::Cratis.Scene.Model.Elements.ExternalComponent
        {
            Id = "selectable-projects",
            Name = "Selectable projects",
            ComponentName = "core:table",
            Properties = new Dictionary<string, object?> { ["query"] = "AllProjects", ["typeName"] = "ProjectSummary", ["dataKey"] = "projectId" },
            Slots = new Dictionary<string, IReadOnlyList<global::Cratis.Scene.Model.Elements.SceneElement>>
            {
                ["columns"] = [new global::Cratis.Scene.Model.Elements.ExternalComponent
                {
                    Id = "project-name-column",
                    Name = "Project name",
                    ComponentName = "core:column",
                    Properties = new Dictionary<string, object?> { ["property"] = "name", ["label"] = "Project name" }
                }]
            }
        }));
    }

    static void AddFallbackScreen(JsonObject document, SceneCommandEntry secondary)
    {
        var fallback = document["screens"]![0]!.DeepClone();
        fallback["name"] = "Fallback";
        fallback["slotContent"]![DefaultLayout.ContentSlotName] = new JsonArray(CanonicalSceneJson.ToNode(new global::Cratis.Scene.Model.Elements.ExternalComponent
        {
            Id = "fallback-secondary-command",
            Name = "Fallback secondary",
            ComponentName = "core:toolbarItem",
            Properties = new Dictionary<string, object?>
            {
                ["label"] = "Fallback secondary",
                ["kind"] = "Action",
                ["target"] = secondary.Key
            }
        }));
        document["screens"]!.AsArray().Add(fallback);
    }
}
