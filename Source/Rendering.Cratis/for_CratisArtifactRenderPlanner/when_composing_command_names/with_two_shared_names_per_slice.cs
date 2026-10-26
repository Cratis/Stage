// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.Scene;
using Cratis.Stage.Rendering.Cratis.Semantics;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_composing_command_names;

public class with_two_shared_names_per_slice : given.a_two_module_request
{
    string[] _keys = null!;
    ArtifactRenderPlan _plan = null!;

    void Establish()
    {
        DuplicateCommandName();
        AddSecondSharedCommand();
        CarryAuthoredScene(true, includeToolbar: true);
        _keys = [.. new SemanticApplicationContext(_request, _options).SceneCommands.ByKey.Keys];
    }

    void Because() => _plan = new CratisArtifactRenderPlanner().Plan(_request);

    string Content(string path) => Text(_plan.Artifacts.Single(artifact => artifact.RelativePath == path));

    [Fact] void should_plan_successfully() => Assert.True(_plan.Success, string.Join(Environment.NewLine, _plan.Diagnostics));
    [Fact] void should_allocate_four_distinct_keys() => _keys.Length.ShouldEqual(4);
    [Fact] void should_emit_every_registry_entry() => _keys.All(key => Content(SceneBindingsRenderer.RelativePath).Contains($"\"{key}\": __sceneCommand", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_emit_every_route_entry() => _keys.All(key => Content(StageModuleRenderer.RelativePath).Contains($"\"{key}\": new __stageCommand", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_emit_every_schema() => JsonNode.Parse(Content(StageSceneDocument.RelativePath))!["commandSchemas"]!.AsObject().Count.ShouldEqual(4);
    [Fact] void should_supply_scene_schemas_to_the_runtime_routes() => Content(StageModuleRenderer.RelativePath).ShouldContain("commandSchemas: (scene as unknown as StageSceneApplication).commandSchemas,");
    [Fact] void should_emit_one_default_form_per_command() => _keys.All(key => Content(SceneCompositionInput.RelativePath).Contains($"\"command\":\"{key}\"", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_keep_the_first_command_key_in_structure()
    {
        var scene = JsonNode.Parse(Content(SceneCompositionInput.RelativePath))!;
        var commands = scene["structure"]!["modules"]!.AsArray()
            .SelectMany(module => module!["features"]!.AsArray())
            .SelectMany(feature => feature!["slices"]!.AsArray())
            .Where(slice => slice!["command"] is not null)
            .Select(slice => slice!["command"]!.GetValue<string>()).ToArray();
        commands.Length.ShouldEqual(2);
        commands.All(_keys.Contains).ShouldBeTrue();
    }
}
