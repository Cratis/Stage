// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Specifications;
using Cratis.Stage.Api;
using Cratis.Stage.Contracts;
using Cratis.Stage.Contracts.Semantics;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Cratis.Stage.Host.for_StageRoutes;

public class when_describing_screen_availability : for_SemanticHost.given.a_model_path
{
    StageRoutes _routes = null!;
    IReadOnlyDictionary<string, string> _expected = null!;
    string _json = string.Empty;

    async Task Establish() => await File.WriteAllTextAsync(_path,
        "audience Plus\n  description \"Plus customers\"\n" +
        Cratis.Stage.Api.for_SceneSynthesizer.given.a_scene_model.Source +
        "\n        screen Restricted\n          available for Plus\n        screen Public\n");

    async Task Because()
    {
        var loaded = await SemanticModelLoader.LoadFromPathAsync(_path);
        var legacy = await EventModelLoader.LoadStageApplicationFromPathAsync(_path);
        var scene = SceneSynthesizer.Synthesize(legacy.Scene, loaded.Model);
        await using var app = WebApplication.CreateBuilder().Build();
        var routes = new StageSceneRoutes(scene, app.Services, NullLogger.Instance);
        _routes = StageRoutes.For(routes, loaded.Model);
        _expected = ScreenAvailabilityParts.For(loaded.Model.Application);
        _json = JsonSerializer.Serialize(_routes, JsonSerializerOptions.Web);
    }

    [Fact] void should_publish_the_generated_marker_key_in_live_routes() => _routes.Screens["Restricted"].ShouldEqual(_expected["Restricted"]);
    [Fact] void should_leave_ungated_screens_out_of_the_map() => _routes.Screens.ContainsKey("Public").ShouldBeFalse();
    [Fact] void should_serialize_the_screen_map_for_the_runtime() => _json.ShouldContain("\"screens\":{\"Restricted\":\"StageScreen_");
}
