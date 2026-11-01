// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Chronicle;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Api;
using Cratis.Stage.Contracts;
using Cratis.Stage.Contracts.Execution;
using Cratis.Stage.Contracts.Scene;
using Cratis.Stage.Contracts.Semantics;
using Cratis.Stage.Host.Published;
using Cratis.Stage.Host.Workbench;
using Cratis.Stage.Runtime;
using Cratis.Stage.Semantics;
using Microsoft.AspNetCore.HttpOverrides;
using Scalar.AspNetCore;

namespace Cratis.Stage.Host;

internal static class SemanticHost
{
    internal static async Task Run(string[] args, string modelPath, PublishedPackage? package = null, PublishedOptions? published = null)
    {
        var issues = new List<StageUnsupportedIssue>();
        var notes = new List<SemanticModelLoadDiagnostic>();
        var (loaded, scene) = await LoadModel(modelPath, issues, notes);

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
        var bodySettings = SemanticBodyWorkerOptions.From(builder.Configuration);
        var bodies = loaded is null ? null : await SemanticBodyWorkerHosting.Start(loaded, modelPath, bodySettings, notes);
        var admission = CreateAdmission(loaded, bodies, issues);
        var surface = loaded is not null && issues.Count == 0 ? StageHttpSurface.Create(loaded.Model, routeOptions) : null;
        builder.AddStageCratis(eventStore, $"Cratis Stage ({eventStore})", routeOptions, published);
        builder.Services.AddSingleton(new StageEventStoreName(eventStore));
        if (surface is not null)
        {
            SemanticRuntimeHosting.Add(builder.Services, loaded!.Plan, WorldProvider(() => world), bodies);
            builder.Services.AddSingleton(bodySettings);
            builder.Services.AddSingleton<ISemanticFactReader, SemanticFactReader>();
            builder.Services.AddSingleton<DynamicTypeFactory>();
            builder.Services.AddHttpContextAccessor();
            builder.Services.AddControllers();
            if (published is null)
            {
                builder.Services.AddSingleton<ISpecificationRunProcess, SpecificationRunProcess>();
                builder.Services.AddSingleton(SemanticSpecificationProcessOptions.FromConfiguration(builder.Configuration));
                builder.Services.AddWorkbenchProxy();
                builder.Services.AddOpenApi(options => options.AddDocumentTransformer<StageOnlyOperationsDocumentTransformer>());
            }
        }

        var extensions = SelectExtensions(published, package);
        var hostContext = published is null ? null : new PublishedHostContext(builder, published, package!.Manifest, eventStore);
        foreach (var extension in extensions)
        {
            extension.ConfigureServices(hostContext!);
        }

        builder.Services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedHost | ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedPrefix;
            options.KnownIPNetworks.Clear();
            options.KnownProxies.Clear();
            foreach (var network in published?.Networks() ?? [])
            {
                options.KnownIPNetworks.Add(new System.Net.IPNetwork(network.Prefix, network.Length));
            }
        });
        var app = builder.Build();
        if (published is not null)
        {
            // Before anything can serve or authenticate: tooling paths answer 404, and only the sign-in proxy's principal counts.
            app.UseToolingRefusal();
            app.UseTrustedProxyPrincipalGuard(published.Networks());
        }

        app.UseSandboxIdentityHeaders();
        app.UseForwardedHeaders();
        app.UseDefaultFiles();
        app.UseStaticFiles(StageStaticFileOptions.Create());
        app.UseRouting();
        app.UseCratisChronicle();
        if (published is null)
        {
            StagePackageEndpoints.Map(app);
            app.MapPost("/stage/load", () => Results.Conflict());
            app.MapGet("/stage/semantic/admission", () => Results.Json(new { engine = "semantic", issues = issues.Select(issue => issue.Details), entries = admission?.Entries ?? [] }, StageJson.Options));
            MapReadModelBrowserPage(app);
        }
        foreach (var entry in admission?.Entries.Where(entry => entry.Status == "renderedAround") ?? [])
        {
            StageLog.RenderedAround(app.Logger, entry.Kind, entry.Artifact, entry.Details!);
        }
        if (scene is not null && loaded is not null)
        {
            MapSceneEndpoints(app, scene, modelPath, loaded.Model);
        }

        if (surface is null && published is not null)
        {
            throw new PublishedModeRefused([.. issues.Select(issue => $"{issue.Capability} {issue.Artifact}: {issue.Details}").DefaultIfEmpty("The model could not be loaded.")]);
        }

        if (surface is null)
        {
            MapRefused(app, issues, notes: notes.Count > 0 ? notes : null);
            await app.RunAsync();
            return;
        }

        var modelName = loaded!.Model.Application.Modules.FirstOrDefault()?.Name ?? "EventModel";
        MapStatus(app, () => RegistrationStatus(world, issues, app.Services, modelName, modelPath), notes);
        if (published is null)
        {
            SemanticSpecificationRuns.Map(app, loaded.Plan, modelPath);
            SemanticReadModelBrowser.Map(app, new(app.Services.GetRequiredService<ISemanticRuntime>(), () => world is not null, context => issues.Count > 0 ? RefusedResponse(context, issues) : null, admission));
        }
        else
        {
            ModelReferencesEndpoint.Map(app, package!.Manifest);
        }

        UseReadinessGate(app, () => world, issues);
        app.Use((context, next) => SemanticUnsupportedResponses.Rewrite(context, () => next(context)));
        app.UseWebSockets();
        app.MapControllers();
        StageEndpointMapper.Map(app, surface);
        if (published is null)
        {
            app.UseSandboxIdentity(modelPath, loaded.Model, surface);
        }

        app.UseCratisArc();
        if (published is null)
        {
            app.MapOpenApi();
            app.MapScalarApiReference(options => options.WithDynamicBaseServerUrl());
            app.MapWorkbenchProxy(WorkbenchAddress.For(app.Services));
        }

        foreach (var extension in extensions)
        {
            extension.MapEndpoints(app, hostContext!);
        }

        app.MapFallbackToFile("index.html");
        if (published is not null)
        {
            PublishedSurface.EnsureNoTooling(app);
        }

        await app.StartAsync();
        try
        {
            var seeds = SemanticSeeds.Read(modelPath, loaded.Plan, bodies);
            foreach (var diagnostic in seeds.Diagnostics)
            {
                StageLog.SeedLeftOut(app.Logger, diagnostic.Code, diagnostic.Message, diagnostic.Source);
            }

            notes.AddRange(seeds.Diagnostics);
            world = await SemanticChronicleRegistration.Register(app.Services.GetRequiredService<IChronicleClient>(), eventStore, new SemanticReplayPlan(loaded.Plan, bodies), seeds);
            await SemanticInbox.Start(app.Services.GetRequiredService<IChronicleClient>(), eventStore, loaded.Plan, (ISemanticForeignFacts)app.Services.GetRequiredService<ISemanticRuntime>());
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

    internal static async Task<(LoadedSemanticModel? Model, SceneApplication? Scene)> LoadModel(string modelPath, List<StageUnsupportedIssue> issues, List<SemanticModelLoadDiagnostic>? notes = null)
    {
        try
        {
            var loaded = await SemanticModelLoader.LoadFromPathAsync(modelPath);
            notes?.AddRange(loaded.Notes);

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
            notes?.AddRange(invalid.Notes);
            issues.AddRange(invalid.Diagnostics.Select(message => new StageUnsupportedIssue("Plan", "model", message)));
            if (issues.Count == 0)
            {
                issues.Add(new StageUnsupportedIssue("Plan", "model", "The semantic model could not be loaded."));
            }

            return (null, null);
        }
    }

    // Endpoint routing ignores trailing slashes. Match the exact path so the canonical URL never redirects to itself.
    internal static void MapReadModelBrowserPage(IApplicationBuilder app) => app.MapWhen(
        context => HttpMethods.IsGet(context.Request.Method) && context.Request.Path == "/read-models/",
        branch => branch.Run(context => Results.Redirect($"../read-models{context.Request.QueryString}").ExecuteAsync(context)));

    internal static void MapSceneEndpoints(WebApplication app, SceneApplication scene, string modelPath, ExecutableSemanticModel model)
    {
        var routes = new StageSceneRoutes(scene, app.Services, app.Logger);
        var strings = new StageStrings(modelPath);

        // The EventModel visitor names the application after its first modeled module (or EventModel
        // when none is declared), rather than after the source folder used by the semantic compiler.
        app.MapGet("/stage/scene", () => Results.Json(routes.Scene.ForRuntime(), StageSceneJson.Options));
        app.MapGet("/stage/routes", () => Results.Json(StageRoutes.For(routes, model), StageJson.Options));
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
            Engine = "semantic",
            Issues = [new StageUnsupportedIssue("World", "model", reason)]
        }
        : new StageStatus("ready", new StageStatusModel(modelName), WarmStageHandoff.ReadHandoffId(modelPath)) { Engine = "semantic" };

    internal static void MapStatus(WebApplication app, Func<StageStatus> status, IReadOnlyList<SemanticModelLoadDiagnostic>? notes = null) =>
        app.MapGet("/stage/status", () => status() with { Notes = notes is { Count: > 0 } ? notes : null });

    internal static void MapRefused(WebApplication app, List<StageUnsupportedIssue> issues, string engine = "semantic", IReadOnlyList<SemanticModelLoadDiagnostic>? notes = null)
    {
        MapStatus(app, () => new StageStatus("unsupported", null, null) { Engine = engine, Issues = issues }, notes);
        app.MapPost(SemanticSpecificationRuns.Route, (HttpContext context) => RefusedResponse(context, issues));
        if (engine == "semantic")
        {
            app.MapGet(SemanticReadModelBrowser.Route, (HttpContext context) => RefusedResponse(context, issues));
            app.MapGet($"{SemanticReadModelBrowser.Route}/{{id}}", (HttpContext context, string id) => RefusedResponse(context, issues));
            app.MapFallbackToFile("/read-models", "index.html");
        }
        MapRefusedApi(app, issues);
    }

    internal static IResult RefusedResponse(HttpContext context, List<StageUnsupportedIssue> issues)
    {
        var issue = issues[0];
        context.Response.Headers["Stage-Unsupported-Capability"] = issue.Capability;
        context.Response.Headers["Stage-Unsupported-Artifact"] = issue.Artifact;

        return Results.Json(new CommandResult { ExceptionMessages = issues.Select(entry => $"Unsupported({entry.Capability}) {entry.Artifact}: {entry.Details}") }, statusCode: StatusCodes.Status501NotImplemented);
    }

    static IReadOnlyList<IPublishedHostExtension> SelectExtensions(PublishedOptions? published, PublishedPackage? package) => published is null
        ? []
        : PublishedHostExtensions.Select(PublishedHostExtensions.Registered, PublishedHostExtensions.NotesEnabled(published, package!.Manifest));

    static SemanticRuntimeAdmission? CreateAdmission(LoadedSemanticModel? loaded, SemanticBodyExecution? bodies, List<StageUnsupportedIssue> issues)
    {
        if (loaded is null) return null;
        var admission = new SemanticRuntimeAdmission(loaded.Plan, id => loaded.ImplementationRequirements.FirstOrDefault(requirement => requirement.RequirementId == id)?.Member, bodies);
        issues.AddRange(admission.Blocking.Select(entry => new StageUnsupportedIssue(entry.Capability!, entry.Artifact, entry.Details!)));
        return admission;
    }

    static void MapRefusedApi(WebApplication app, List<StageUnsupportedIssue> issues)
    {
        app.MapMethods("/api/{**path}", ["GET", "POST", "PUT", "DELETE", "PATCH", "QUERY"], (HttpContext context, string path) => RefusedResponse(context, issues))
            .ExcludeFromDescription();
    }
}
