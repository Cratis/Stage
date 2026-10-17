// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Specifications;
using Cratis.Stage.Api;
using Cratis.Stage.Contracts;
using Cratis.Stage.Contracts.Scene;
using Cratis.Stage.Contracts.Semantics;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Cratis.Stage.Host.for_StageRoutes;

public class when_describing_commands_without_toolbar_slices : for_SemanticHost.given.a_model_path
{
    StageRoutes _legacy = null!;
    StageRoutes _semantic = null!;
    SceneApplication _scene = null!;

    async Task Establish() => await File.WriteAllTextAsync(_path,
        Cratis.Stage.Api.for_SceneSynthesizer.given.a_scene_model.Source + "\n        screen Overview\n          action \"Register\"\n            when item.name == \"Ready\" execute RegisterProject\n            otherwise hidden");

    async Task Because()
    {
        var legacy = await EventModelLoader.LoadStageApplicationFromPathAsync(_path);
        var semantic = await SemanticModelLoader.LoadFromPathAsync(_path);
        _scene = SceneSynthesizer.Synthesize(legacy.Scene, legacy.EventModel);
        await using var app = WebApplication.CreateBuilder().Build();
        var routes = new StageSceneRoutes(_scene, app.Services, NullLogger.Instance);
        _legacy = StageRoutes.For(routes, legacy.EventModel);
        _semantic = StageRoutes.For(routes, semantic.Model);
    }

    [Fact] void should_keep_schemas_out_of_the_scene_without_toolbar_slices() => _scene.CommandSchemas.ShouldBeNull();
    [Fact] void should_expose_the_same_schemas_from_both_live_engines() => JsonSerializer.Serialize(_semantic.CommandSchemas).ShouldEqual(JsonSerializer.Serialize(_legacy.CommandSchemas));
    [Fact] void should_describe_object_echo_responses_in_both_engines() => JsonSerializer.Serialize(_semantic.CommandResponseKinds).ShouldEqual(JsonSerializer.Serialize(_legacy.CommandResponseKinds));
    [Fact] void should_preserve_the_legacy_object_echo() => _legacy.CommandResponseKinds["RegisterProject"].ShouldEqual("object");
    [Fact] void should_describe_the_guarded_command_even_without_a_form() => _semantic.CommandSchemas["RegisterProject"].ShouldContain("\"properties\"");
}
