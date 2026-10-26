// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_composing_command_names;

public class with_ambiguous_names : given.a_two_module_request
{
    ArtifactRenderPlan _plan = null!;
    string[] _keys = null!;

    void Establish()
    {
        DuplicateCommandName();
        CarryAuthoredScene(true);
    }

    void Because()
    {
        _plan = _planner.Plan(_request);
        var scene = JsonNode.Parse(Text(_plan.Artifacts.Single(artifact => artifact.RelativePath == "src/stage-scene.json")))!;
        _keys = [.. scene["structure"]!["modules"]!.AsArray().SelectMany(module => module!["features"]!.AsArray().SelectMany(feature => feature!["slices"]!.AsArray()
            .Where(slice => slice!["command"] is not null).Select(slice => slice!["command"]!.GetValue<string>())))];
    }

    [Fact] void should_admit_shared_names_in_distinct_slices() => _plan.Success.ShouldBeTrue();
    [Fact] void should_register_keys_matching_the_emitted_structure() => _keys.All(key => Text(_plan.Artifacts.Single(artifact => artifact.RelativePath == "src/bindings.ts")).Contains($"\"{key}\":", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_compose_both_command_forms() => _keys.All(key => Text(_plan.Artifacts.Single(artifact => artifact.RelativePath == "scene.json")).Contains($"\"command\":\"{key}\"", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_emit_all_shared_user_interface() => UserInterfacePaths.All(path => _plan.Artifacts.Any(artifact => artifact.RelativePath == path)).ShouldBeTrue();
}
