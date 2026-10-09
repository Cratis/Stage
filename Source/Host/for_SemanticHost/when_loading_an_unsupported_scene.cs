// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts;
using Cratis.Stage.Contracts.Scene;
using Cratis.Stage.Contracts.Semantics;
using Microsoft.AspNetCore.Routing;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticHost;

public class when_loading_an_unsupported_scene : given.a_model_path
{
    [Theory]
    [InlineData("semantic", "route \"projects/overview\"")]
    [InlineData("semantic", "parameter name from data.name")]
    [InlineData("eventmodel", "route \"projects/overview\"")]
    [InlineData("eventmodel", "parameter name from data.name")]
    public async Task should_record_scene_refusals_instead_of_crashing_the_host(string engine, string navigation)
    {
        var screen = $$"""
                screen Overview
                  navigate to Overview
                    {{navigation}}
        """;
        await File.WriteAllTextAsync(_path, Cratis.Stage.Api.for_SceneSynthesizer.given.a_scene_model.Source + "\n" + screen);
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
        issue.Artifact.ShouldEqual("model");
        const string diagnosticCode = UnsupportedUiSyntax.DiagnosticCode;
        issue.Details.ShouldContain(diagnosticCode);
        issue.Details.ShouldContain("ScreenNavigateSyntax.Route/Parameters");

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
            if (path.StartsWith("/api", StringComparison.Ordinal))
            {
                context.Response.StatusCode.ShouldEqual(StatusCodes.Status501NotImplemented);
                context.Response.Headers["Stage-Unsupported-Capability"].ToString().ShouldEqual("Scene");
            }
            else
            {
                context.Response.StatusCode.ShouldEqual(StatusCodes.Status200OK);
                response.ShouldContain("unsupported");
                response.ShouldContain(engine);
            }
            response.ShouldContain(diagnosticCode);
        }
    }
}
