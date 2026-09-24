// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Chronicle;
using Cratis.Stage.Api;
using Cratis.Stage.Contracts;
using Cratis.Stage.Contracts.Scene;
using Cratis.Stage.Contracts.Semantics;
using Cratis.Stage.Host.Workbench;
using Cratis.Stage.Runtime;
using Cratis.Stage.Semantics;
using Microsoft.AspNetCore.HttpOverrides;
using Scalar.AspNetCore;

namespace Cratis.Stage.Host;

internal static class SemanticHost
{
    internal static async Task Run(string[] args, string modelPath)
    {
        LoadedSemanticModel? loaded = null;
        var issues = new List<string>();
        try
        {
            loaded = await SemanticModelLoader.LoadFromPathAsync(modelPath);
        }
        catch (InvalidSemanticModel invalid)
        {
            issues.AddRange(invalid.Diagnostics);
        }

        var admission = loaded is null ? null : new SemanticRuntimeAdmission(loaded.Plan);
        if (admission is not null)
        {
            issues.AddRange(admission.Blocking.Select(entry => $"{entry.Artifact}: {entry.Details}"));
        }

        var eventStore = ContainerEventStoreName.Resolve();
        var builder = WebApplication.CreateBuilder(args);
        builder.Configuration.AddJsonFile(
            Environment.GetEnvironmentVariable("STAGE_CONFIG") is { Length: > 0 } configuredPath
                ? configuredPath
                : Path.Combine(builder.Environment.ContentRootPath, "cratis-stage.json"),
            optional: true,
            reloadOnChange: true);
        var routeOptions = StageHttpRouteOptions.FromConfiguration(builder.Configuration);
        var surface = loaded is not null && issues.Count == 0 ? StageHttpSurface.Create(loaded.Model, routeOptions) : null;
        builder.AddStageCratis(eventStore, $"Cratis Stage ({eventStore})", routeOptions);
        builder.Services.AddSingleton(new StageEventStoreName(eventStore));
        if (surface is not null)
        {
            SemanticRuntimeHosting.Add(builder.Services, loaded!.Plan);
            builder.Services.AddSingleton<DynamicTypeFactory>();
            builder.Services.AddHttpContextAccessor();
            builder.Services.AddControllers();
            builder.Services.AddWorkbenchProxy();
            builder.Services.AddOpenApi(options => options.AddDocumentTransformer<StageOnlyOperationsDocumentTransformer>());
        }

        builder.Services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedHost | ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedPrefix;
            options.KnownIPNetworks.Clear();
            options.KnownProxies.Clear();
        });
        var app = builder.Build();
        app.UseForwardedHeaders();
        app.UseDefaultFiles();
        app.UseStaticFiles();
        app.UseRouting();
        app.UseCratisChronicle();
        app.MapPost("/stage/load", () => Results.Conflict());
        app.MapGet("/stage/semantic/admission", () => Results.Json(new { engine = "semantic", issues, entries = admission?.Entries ?? [] }, StageJson.Options));

        if (surface is null)
        {
            MapRefused(app, issues);
            await app.RunAsync();
            return;
        }

        try
        {
            if (!await SemanticChronicleRegistration.Register(app.Services.GetRequiredService<IChronicleClient>(), eventStore, loaded!.Plan))
            {
                issues.Add("Unsupported(World): The Chronicle event log is not empty; the semantic world cannot be reconstructed.");
            }
        }
        catch (Exception exception)
        {
            issues.Add($"Unsupported(World): Chronicle initialization failed: {exception.Message}");
        }

        if (issues.Count > 0)
        {
            MapRefused(app, issues);
            await app.RunAsync();
            return;
        }

        app.Use((context, next) => SemanticUnsupportedResponses.Rewrite(context, () => next(context)));
        app.UseWebSockets();
        app.MapControllers();
        StageEndpointMapper.Map(app, surface);
        app.UseCratisArc();
        app.MapOpenApi();
        app.MapScalarApiReference(options => options.WithDynamicBaseServerUrl());
        app.MapWorkbenchProxy(WorkbenchAddress.For(app.Services));

        SceneApplication scene;
        try
        {
            var presentation = await EventModelLoader.LoadStageApplicationFromPathAsync(modelPath);
            scene = SceneSynthesizer.Synthesize(presentation.Scene, presentation.EventModel);
        }
        catch (InvalidEventModel)
        {
            scene = await EventModelLoader.LoadSceneApplicationFromDirectoryAsync(Directory.Exists(modelPath) ? modelPath : Path.GetDirectoryName(Path.GetFullPath(modelPath))!);
        }

        var routes = new StageSceneRoutes(scene, app.Services, app.Logger);
        var strings = new StageStrings(modelPath);
        app.MapGet("/stage/status", () => new StageStatus("ready", new StageStatusModel(loaded!.Model.Application.Name), WarmStageHandoff.ReadHandoffId(modelPath)) { Engine = "semantic" });
        app.MapGet("/stage/scene", () => Results.Json(routes.Scene, StageJson.Options));
        app.MapGet("/stage/routes", () => Results.Json(new StageRoutes(routes.CommandRoutes, routes.QueryRoutes), StageJson.Options));
        app.MapGet("/stage/locales", () => Results.Json(strings.Locales(), StageJson.Options));
        app.MapGet("/stage/strings/{locale}", (string locale) => Results.Json(strings.Dictionary(locale), StageJson.Options));
        app.MapFallbackToFile("index.html");
        await app.RunAsync();
    }

    static void MapRefused(WebApplication app, IReadOnlyList<string> issues)
    {
        app.MapGet("/stage/status", () => new StageStatus("unsupported", null, null)
        {
            Engine = "semantic",
            Issues = [.. issues.Select(issue => new StageUnsupportedIssue(issue.StartsWith("Unsupported(World):", StringComparison.Ordinal) ? "World" : "Plan", "model", issue))]
        });
        app.MapMethods("/api/{**path}", ["GET", "POST", "PUT", "DELETE", "PATCH", "QUERY"], (HttpContext context, string path) =>
        {
            context.Response.Headers["Stage-Unsupported-Capability"] = "Plan";
            context.Response.Headers["Stage-Unsupported-Artifact"] = "model";
            return Results.Json(new CommandResult { ExceptionMessages = issues.Select(issue => $"Unsupported(Plan) model: {issue}") }, statusCode: StatusCodes.Status501NotImplemented);
        });
    }
}
