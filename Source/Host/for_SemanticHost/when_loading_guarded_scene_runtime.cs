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
    [InlineData("semantic", "action", "STAGE-SCENE-ACTION-001")]
    [InlineData("eventmodel", "action", "STAGE-SCENE-ACTION-001")]
    [InlineData("semantic", "interaction", "STAGE-SCENE-INTERACTION-001")]
    [InlineData("eventmodel", "interaction", "STAGE-SCENE-INTERACTION-001")]
    public async Task should_record_refusals_and_serve_unsupported_responses(string engine, string kind, string code)
    {
        var directive = kind == "action" ? """
                  action "Register"
                    when item.name == "Ready" execute RegisterProject
                    otherwise hidden
        """ : """
                  on click
                    when item.name == "Ready"
                      execute RegisterProject
                    otherwise
                      notify info "Not ready"
        """;
        await File.WriteAllTextAsync(_path, Cratis.Stage.Api.for_SceneSynthesizer.given.a_scene_model.Source + "\n        screen Overview\n" + directive);
        var issues = new List<StageUnsupportedIssue>();
        LoadedSemanticModel? model = null;
        SceneApplication? scene = null;
        StageApplication? legacy = null;
        var error = await Catch.Exception(async () =>
        {
            if (engine == "semantic") (model, scene) = await SemanticHost.LoadModel(_path, issues);
            else legacy = await EventModelHost.LoadModel(_path, issues);
        });
        error.ShouldBeNull();
        model.ShouldBeNull();
        scene.ShouldBeNull();
        legacy.ShouldBeNull();
        var issue = Assert.Single(issues);
        issue.Capability.ShouldEqual("Scene");
        issue.Details.ShouldContain(code);

        await using var app = WebApplication.CreateBuilder().Build();
        SemanticHost.MapRefused(app, issues, engine);
        var pipeline = new ApplicationBuilder(app.Services);
        pipeline.UseRouting();
        pipeline.UseEndpoints(endpoints =>
        {
            foreach (var source in ((IEndpointRouteBuilder)app).DataSources) endpoints.DataSources.Add(source);
        });
        var request = pipeline.Build();
        await using var scope = app.Services.CreateAsyncScope();
        foreach (var path in new[] { "/stage/status", "/api/commands/RegisterProject" })
        {
            await using var body = new MemoryStream();
            var context = new DefaultHttpContext { RequestServices = scope.ServiceProvider };
            context.Request.Method = "GET";
            context.Request.Path = path;
            context.Response.Body = body;
            await request(context);
            var response = System.Text.Encoding.UTF8.GetString(body.ToArray());
            if (path == "/stage/status")
            {
                context.Response.StatusCode.ShouldEqual(StatusCodes.Status200OK);
                response.ShouldContain("unsupported");
                response.ShouldContain(engine);
            }
            else
            {
                context.Response.StatusCode.ShouldEqual(StatusCodes.Status501NotImplemented);
                context.Response.Headers["Stage-Unsupported-Capability"].ToString().ShouldEqual("Scene");
            }
            response.ShouldContain(code);
        }
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
