// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Nodes;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Scene;
using Xunit;

namespace Cratis.Stage.Api.for_SceneSynthesizer;

public class when_synthesizing_semantic_screens : given.a_scene_model
{
    SceneApplication _legacy = null!;
    SceneApplication _semantic = null!;

    void Because()
    {
        _legacy = SceneSynthesizer.Synthesize(_scene, _eventModel);
        _semantic = SceneSynthesizer.Synthesize(_scene, _semanticModel);
    }

    [Fact] void should_produce_the_same_scene_as_the_event_model_path() => JsonSerializer.Serialize(_semantic with { ResponseContracts = null }).ShouldEqual(JsonSerializer.Serialize(_legacy));
    [Fact] void should_leave_legacy_response_contracts_unset() => _legacy.ResponseContracts.ShouldBeNull();
    [Fact] void should_describe_the_semantic_response_contracts() => JsonNode.DeepEquals(_semantic.ResponseContracts, JsonNode.Parse(ExpectedResponseContracts)).ShouldBeTrue();
    [Fact] void should_synthesize_both_the_command_and_read_model_screens() => _semantic.Screens.Count.ShouldEqual(2);
    [Fact] void should_keep_nested_slice_names() => _semantic.Screens.Select(screen => screen.Name).ShouldContainOnly(["RegisterProject", "ProjectLookup"]);
    [Fact] void should_not_invent_an_application_layout() => _semantic.Layouts.ShouldBeEmpty();
    [Fact] void should_leave_the_shell_for_the_default_blueprint_to_resolve() => _semantic.Screens.All(screen => string.IsNullOrEmpty(screen.Layout)).ShouldBeTrue();
    [Fact] void should_not_invent_a_layout_on_the_event_model_path() => _legacy.Layouts.ShouldBeEmpty();
    [Fact] void should_leave_the_event_model_screens_without_a_layout() => _legacy.Screens.All(screen => string.IsNullOrEmpty(screen.Layout)).ShouldBeTrue();

    const string ExpectedResponseContracts = """
        {
          "commands": { "RegisterProject": { "kind": "void" } },
          "commandInputs": {
            "RegisterProject": {
              "projectId": { "kind": "string" },
              "name": { "kind": "string" },
              "stage": { "kind": "string" },
              "description": { "kind": "string", "nullable": true },
              "tags": { "kind": "array", "item": { "kind": "string" } }
            }
          },
          "screenInputs": {}
        }
        """;
}
