// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Text.Json.Nodes;
using Cratis.Scene.Model.Profiles;
using Cratis.Scene.Model.SampleData;
using Cratis.Scene.Model.Structure;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Scene;
using Cratis.Stage.Rendering.Cratis.Scene;
using Xunit;
using SceneElements = Cratis.Scene.Model.Elements;
using SceneScreens = Cratis.Scene.Model.Screens;

namespace Cratis.Stage.Rendering.Cratis.for_CratisRendering.when_planning_with_a_profile;

public class with_no_toolbar_slice_button : given.a_profile_source_plan
{
    JsonObject _runtime = null!;

    void Establish() => PlanFull();

    void Because() => _runtime = JsonNode.Parse(Encoding.UTF8.GetString(_full.Artifacts.Single(artifact => artifact.RelativePath == StageSceneDocument.RelativePath).Bytes.AsSpan()))!.AsObject();

    [Fact] void should_carry_no_command_schemas() => _runtime.ContainsKey("commandSchemas").ShouldBeFalse();
    [Fact] void should_carry_no_structure() => _runtime.ContainsKey("structure").ShouldBeFalse();
}
