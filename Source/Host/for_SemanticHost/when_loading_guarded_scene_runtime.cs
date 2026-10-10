// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts;
using Cratis.Stage.Contracts.Scene;
using Cratis.Stage.Contracts.Semantics;
using Microsoft.AspNetCore.Routing;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticHost;

public class when_loading_guarded_scene_runtime : given.a_model_path
{
    [Theory]
    [InlineData("semantic")]
    [InlineData("eventmodel")]
    public async Task should_serve_guarded_screen_actions_the_runtime_evaluates(string engine)
    {
        await File.WriteAllTextAsync(_path, Cratis.Stage.Api.for_SceneSynthesizer.given.a_scene_model.Source + "\n        screen Overview\n          action \"Register\"\n            when item.name == \"Ready\" execute RegisterProject\n            otherwise hidden");
        var issues = new List<StageUnsupportedIssue>();

        var scene = await LoadScene(engine, issues);

        scene.ShouldNotBeNull();
        issues.ShouldBeEmpty();
        var sceneJson = System.Text.Json.JsonSerializer.Serialize(scene, StageJson.Options);
        sceneJson.ShouldContain("Overview");
        sceneJson.ShouldContain("\"alternatives\"");
        sceneJson.ShouldContain("\"right\":\"Ready\"");
        sceneJson.ShouldContain("\"outcome\":\"Hidden\"");
    }

    [Theory]
    [InlineData("semantic")]
    [InlineData("eventmodel")]
    public async Task should_serve_the_guarded_action_from_the_scene_endpoint(string engine)
    {
        await File.WriteAllTextAsync(_path, Cratis.Stage.Api.for_SceneSynthesizer.given.a_scene_model.Source + "\n        screen Overview\n          action \"Register\"\n            when item.name == \"Ready\" execute RegisterProject\n            otherwise hidden");
        var issues = new List<StageUnsupportedIssue>();
        var scene = await LoadScene(engine, issues);
        await using var app = WebApplication.CreateBuilder().Build();
        SemanticHost.MapSceneEndpoints(app, scene!, _path);
        var pipeline = new ApplicationBuilder(app.Services);
        pipeline.UseRouting();
        pipeline.UseEndpoints(endpoints =>
        {
            foreach (var source in ((IEndpointRouteBuilder)app).DataSources) endpoints.DataSources.Add(source);
        });
        var request = pipeline.Build();
        await using var scope = app.Services.CreateAsyncScope();

        var sceneResponse = await ResponseFor(request, scope.ServiceProvider, "/stage/scene");
        var routesResponse = await ResponseFor(request, scope.ServiceProvider, "/stage/routes");
        var localesResponse = await ResponseFor(request, scope.ServiceProvider, "/stage/locales");

        sceneResponse.Status.ShouldEqual(StatusCodes.Status200OK);
        sceneResponse.Body.ShouldContain("Overview");
        sceneResponse.Body.ShouldContain("\"alternatives\"");
        sceneResponse.Body.ShouldContain("\"command\":\"RegisterProject\"");
        routesResponse.Status.ShouldEqual(StatusCodes.Status200OK);
        routesResponse.Body.ShouldContain("commands");
        routesResponse.Body.ShouldContain("queries");
        localesResponse.Status.ShouldEqual(StatusCodes.Status200OK);
    }

    [Theory]
    [InlineData("semantic")]
    [InlineData("eventmodel")]
    public async Task should_serve_guarded_interactions_as_one_binding_per_branch(string engine)
    {
        await File.WriteAllTextAsync(_path, Cratis.Stage.Api.for_SceneSynthesizer.given.a_scene_model.Source + """

                screen Overview
                  on click
                    when item.name == "Ready"
                      execute RegisterProject
                    otherwise
                      notify info "Not ready"
        """);
        var issues = new List<StageUnsupportedIssue>();

        var scene = await LoadScene(engine, issues);

        scene.ShouldNotBeNull();
        issues.ShouldBeEmpty();
        var sceneJson = System.Text.Json.JsonSerializer.Serialize(scene, StageJson.Options);
        sceneJson.ShouldContain("Overview");
        sceneJson.ShouldContain(SceneGuards.GuardPath);
        sceneJson.ShouldContain("\"kind\":\"firstMatch\"");
        sceneJson.ShouldContain("\"kind\":\"otherwise\"");
        sceneJson.ShouldContain("Not ready");
    }

    async Task<SceneApplication?> LoadScene(string engine, List<StageUnsupportedIssue> issues)
    {
        if (engine == "semantic")
        {
            var (model, scene) = await SemanticHost.LoadModel(_path, issues);
            model.ShouldNotBeNull();
            return scene;
        }

        return (await EventModelHost.LoadModel(_path, issues))?.Scene;
    }

    static async Task<(int Status, string Body)> ResponseFor(RequestDelegate request, IServiceProvider services, string path)
    {
        await using var body = new MemoryStream();
        var context = new DefaultHttpContext { RequestServices = services };
        context.Request.Method = "GET";
        context.Request.Path = path;
        context.Response.Body = body;
        await request(context);
        return (context.Response.StatusCode, System.Text.Encoding.UTF8.GetString(body.ToArray()));
    }

    [Theory]
    [InlineData("semantic")]
    [InlineData("eventmodel")]
    public async Task should_keep_unguarded_scenes_available(string engine)
    {
        await File.WriteAllTextAsync(_path, Cratis.Stage.Api.for_SceneSynthesizer.given.a_scene_model.Source + "\n        screen Overview\n          action RegisterProject\n          on click\n            execute RegisterProject");
        var issues = new List<StageUnsupportedIssue>();
        SceneApplication? scene;
        if (engine == "semantic") (_, scene) = await SemanticHost.LoadModel(_path, issues);
        else scene = (await EventModelHost.LoadModel(_path, issues))?.Scene;
        scene.ShouldNotBeNull();
        issues.ShouldBeEmpty();
    }
}
