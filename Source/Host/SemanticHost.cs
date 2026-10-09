// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Chronicle;
using Cratis.Screenplay.Semantics.Execution;
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
        var issues = new List<StageUnsupportedIssue>();
        var (loaded, scene) = await LoadModel(modelPath, issues);

        var admission = loaded is null ? null : new SemanticRuntimeAdmission(loaded.Plan);
        if (admission is not null)
        {
            issues.AddRange(admission.Blocking.Select(entry => new StageUnsupportedIssue(entry.Capability!, entry.Artifact, entry.Details!)));
        }

        var eventStore = ContainerEventStoreName.Resolve();
        SemanticWorld? world = null;
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
            SemanticRuntimeHosting.Add(builder.Services, loaded!.Plan, WorldProvider(() => world));
            builder.Services.AddSingleton<ISpecificationRunProcess, SpecificationRunProcess>();
            builder.Services.AddSingleton(SemanticSpecificationProcessOptions.FromConfiguration(builder.Configuration));
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
        app.UseStaticFiles(StageStaticFileOptions.Create());
        app.UseRouting();
        app.UseCratisChronicle();
        app.MapPost("/stage/load", () => Results.Conflict());
        app.MapGet("/stage/semantic/admission", () => Results.Json(new { engine = "semantic", issues = issues.Select(issue => issue.Details), entries = admission?.Entries ?? [] }, StageJson.Options));

        if (surface is null)
        {
            MapRefused(app, issues);
            await app.RunAsync();
            return;
        }

        var modelName = loaded!.Model.Application.Modules.FirstOrDefault()?.Name ?? "EventModel";
        app.MapGet("/stage/status", () => RegistrationStatus(world, issues, app.Services, modelName, modelPath));
        SemanticSpecificationRuns.Map(app, loaded.Plan, modelPath);
        UseReadinessGate(app, () => world, issues);
        app.Use((context, next) => SemanticUnsupportedResponses.Rewrite(context, () => next(context)));
        app.UseWebSockets();
        app.MapControllers();
        StageEndpointMapper.Map(app, surface);
        app.UseCratisArc();
        app.MapOpenApi();
        app.MapScalarApiReference(options => options.WithDynamicBaseServerUrl());
        app.MapWorkbenchProxy(WorkbenchAddress.For(app.Services));

        MapSceneEndpoints(app, scene!, modelPath);
        app.MapFallbackToFile("index.html");
        await app.StartAsync();
        try
        {
            world = await SemanticChronicleRegistration.Register(app.Services.GetRequiredService<IChronicleClient>(), eventStore, loaded.Plan);
        }
        catch (SemanticWorldRebuildRefused exception)
        {
            issues.Add(new StageUnsupportedIssue("World", "model", $"World rebuild refused: {exception.Message}"));
        }
        catch (Exception exception)
        {
            issues.Add(new StageUnsupportedIssue("World", "model", $"Chronicle initialization failed: {exception.Message}"));
        }

        await app.WaitForShutdownAsync();
    }

    internal static async Task<(LoadedSemanticModel? Model, SceneApplication? Scene)> LoadModel(string modelPath, List<StageUnsupportedIssue> issues)
    {
        try
        {
            var loaded = await SemanticModelLoader.LoadFromPathAsync(modelPath);

            var scene = SemanticHostScene.Load(modelPath, loaded.Model);
            if (scene.RuntimeIssues.All(SafeSceneApplication.CanServeWithoutUnsafeBehavior))
            {
                return (loaded, SafeSceneApplication.From(scene));
            }

            issues.AddRange(scene.RuntimeIssues.Select(issue => new StageUnsupportedIssue("Scene", issue.Artifact, issue.Details)));

            return scene.RuntimeIssues.Count > 0 ? (null, null) : (loaded, scene);
        }
        catch (Exception exception) when (exception is UnsupportedUiSyntax)
        {
            issues.Add(new StageUnsupportedIssue("Scene", "model", exception.Message));

            return (null, null);
        }
        catch (InvalidSemanticModel invalid)
        {
            issues.AddRange(invalid.Diagnostics.Select(message => new StageUnsupportedIssue("Plan", "model", message)));
            if (issues.Count == 0)
            {
                issues.Add(new StageUnsupportedIssue("Plan", "model", "The semantic model could not be loaded."));
            }

            return (null, null);
        }
    }

    internal static void MapSceneEndpoints(WebApplication app, SceneApplication scene, string modelPath)
    {
        var routes = new StageSceneRoutes(scene, app.Services, app.Logger);
        var strings = new StageStrings(modelPath);

        // The EventModel visitor names the application after its first modeled module (or EventModel
        // when none is declared), rather than after the source folder used by the semantic compiler.
        app.MapGet("/stage/scene", () => Results.Json(routes.Scene, StageJson.Options));
        app.MapGet("/stage/routes", () => Results.Json(new StageRoutes(routes.CommandRoutes, routes.QueryRoutes), StageJson.Options));
        app.MapGet("/stage/locales", () => Results.Json(strings.Locales(), StageJson.Options));
        app.MapGet("/stage/strings/{locale}", (string locale) => Results.Json(strings.Dictionary(locale), StageJson.Options));
    }

    internal static void UseReadinessGate(IApplicationBuilder app, Func<SemanticWorld?> world, List<StageUnsupportedIssue> issues) => app.Use(async (context, next) =>
    {
        if (context.Request.Path.StartsWithSegments("/api") && (world() is null || issues.Count > 0))
        {
            if (issues.Count > 0)
            {
                var issue = issues[0];
                context.Response.Headers["Stage-Unsupported-Capability"] = issue.Capability;
                context.Response.Headers["Stage-Unsupported-Artifact"] = issue.Artifact;
                await Results.Json(new CommandResult { ExceptionMessages = issues.Select(entry => $"Unsupported({entry.Capability}) {entry.Artifact}: {entry.Details}") }, statusCode: StatusCodes.Status501NotImplemented).ExecuteAsync(context);
            }
            else
            {
                context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            }

            return;
        }

        await next(context);
    });

    internal static Func<SemanticWorld> WorldProvider(Func<SemanticWorld?> world) =>
        () => world() ?? throw new SemanticWorldRebuildRefused("The semantic world has not been reconstructed yet.");

    internal static StageStatus RegistrationStatus(SemanticWorld? world, List<StageUnsupportedIssue> issues, IServiceProvider services, string modelName, string modelPath)
    {
        if (issues.Count > 0)
        {
            return new StageStatus("unsupported", null, WarmStageHandoff.ReadHandoffId(modelPath)) { Engine = "semantic", Issues = issues };
        }

        return world is null
            ? new StageStatus("loading", null, WarmStageHandoff.ReadHandoffId(modelPath)) { Engine = "semantic" }
            : Status(services.GetRequiredService<ISemanticRuntime>(), modelName, modelPath);
    }

    internal static StageStatus Status(ISemanticRuntime runtime, string modelName, string modelPath) => runtime is ISemanticRuntimeStatus { FaultReason: { } reason }
        ? new StageStatus("unsupported", null, WarmStageHandoff.ReadHandoffId(modelPath))
        {
            Engine = "semantic", Issues = [new StageUnsupportedIssue("World", "model", reason)]
        }
        : new StageStatus("ready", new StageStatusModel(modelName), WarmStageHandoff.ReadHandoffId(modelPath)) { Engine = "semantic" };

    internal static void MapRefused(WebApplication app, List<StageUnsupportedIssue> issues, string engine = "semantic")
    {
        app.MapGet("/stage/status", () => new StageStatus("unsupported", null, null)
        {
            Engine = engine, Issues = issues
        });
        app.MapPost(SemanticSpecificationRuns.Route, (HttpContext context) => RefusedResponse(context, issues));
        MapRefusedApi(app, issues);
    }

    static void MapRefusedApi(WebApplication app, List<StageUnsupportedIssue> issues)
    {
        app.MapMethods("/api/{**path}", ["GET", "POST", "PUT", "DELETE", "PATCH", "QUERY"], (HttpContext context, string path) => RefusedResponse(context, issues));
    }

    static IResult RefusedResponse(HttpContext context, List<StageUnsupportedIssue> issues)
    {
        var issue = issues[0];
        context.Response.Headers["Stage-Unsupported-Capability"] = issue.Capability;
        context.Response.Headers["Stage-Unsupported-Artifact"] = issue.Artifact;

        return Results.Json(new CommandResult { ExceptionMessages = issues.Select(entry => $"Unsupported({entry.Capability}) {entry.Artifact}: {entry.Details}") }, statusCode: StatusCodes.Status501NotImplemented);
    }
}
