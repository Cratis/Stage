// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using Cratis.Chronicle;
using Cratis.Chronicle.EventSequences;
using Cratis.Stage.Api;
using Cratis.Stage.Contracts;
using Cratis.Stage.Host;
using Cratis.Stage.Host.Workbench;
using Cratis.Stage.Runtime;
using Microsoft.AspNetCore.HttpOverrides;
using Scalar.AspNetCore;

CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;
CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;

var modelPath = args.FirstOrDefault(argument => !argument.StartsWith('-'));
var warmMode = args.Contains("--warm", StringComparer.Ordinal) ||
               (modelPath is null && bool.TryParse(Environment.GetEnvironmentVariable("STAGE_WARM"), out var warm) && warm);

if (!warmMode && modelPath is null)
{
    throw new MissingModelArgument();
}

var stageApplication = warmMode ? null : await EventModelLoader.LoadStageApplicationFromPathAsync(modelPath!);
var model = stageApplication?.EventModel;
var scene = stageApplication?.Scene;
if (model is not null)
{
    // Registration starts asynchronously after ApplicationStarted. Validate pure definitions before the host
    // listens or reports ready, then the registrar validates again before it attempts any remote writes.
    _ = StageChronicleDefinitions.BuildEventTypes(model);
    _ = StageChronicleDefinitions.Build(model, EventSequenceId.Log);
}
var eventStore = ContainerEventStoreName.Resolve();
var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile(
    Environment.GetEnvironmentVariable("STAGE_CONFIG") is { Length: > 0 } configuredPath
        ? configuredPath
        : Path.Combine(builder.Environment.ContentRootPath, "cratis-stage.json"),
    optional: true,
    reloadOnChange: true);

// Admit modeled ownership before AddCratis, DI/provider resolution, type emission, any endpoint mapping,
// or Chronicle connection. Use one startup snapshot for planning, mapping, and discovery.
var routeOptions = StageHttpRouteOptions.FromConfiguration(builder.Configuration);
var httpSurface = model is null ? null : StageHttpSurface.Create(model, routeOptions);
builder.AddStageCratis(eventStore, programIdentifier: $"Cratis Stage ({eventStore})", routeOptions: routeOptions);

if (model is not null)
{
    builder.Services.AddSingleton(model);
    builder.Services.AddSingleton<DynamicTypeFactory>();
    builder.Services.AddSingleton<IAppendProducedEvents, ProducedEventAppender>();
    builder.Services.AddSingleton<IProvideStageIdentity, StageIdentity>();
    builder.Services.AddHttpContextAccessor();
    builder.Services.AddControllers();
    builder.Services.AddWorkbenchProxy();
    builder.Services.AddOpenApi(options => options.AddDocumentTransformer<StageOnlyOperationsDocumentTransformer>());
}

builder.Services.AddSingleton<StageEventStoreName>(eventStore);
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedHost | ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedPrefix;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

var app = builder.Build();
app.UseForwardedHeaders();
if (!warmMode)
{
    app.UseDefaultFiles();
    app.UseStaticFiles(new StaticFileOptions
    {
        OnPrepareResponse = context =>
        {
            context.Context.Response.Headers.CacheControl = context.Context.Request.Path.StartsWithSegments("/assets")
                ? "public,max-age=31536000,immutable"
                : "no-store";
        }
    });
}
app.UseRouting();
app.UseCratisChronicle();

if (warmMode)
{
    using var handoff = new WarmStageHandoff("/eventmodel");

    app.MapGet("/stage/status", handoff.GetStatus);
    app.MapPost("/stage/load", async (StageLoadRequest request, IChronicleClient chronicleClient, CancellationToken cancellationToken) =>
    {
        var result = await handoff.Load(
            request,
            _ => ResetKernel(chronicleClient, eventStore),
            cancellationToken);

        if (result == StageHandoffResult.Conflict)
        {
            return Results.Conflict();
        }

        if (result == StageHandoffResult.InvalidPath)
        {
            return Results.BadRequest();
        }

        Environment.ExitCode = 42;
        _ = Task.Run(async () =>
        {
            await Task.Delay(TimeSpan.FromMilliseconds(100));
            app.Lifetime.StopApplication();
        });

        return Results.Accepted();
    });

    await app.RunAsync();
    return;
}

StageLog.Running(app.Logger, model!.Name, eventStore);
app.UseWebSockets();
app.MapControllers();
StageEndpointMapper.Map(app, httpSurface!);
app.UseCratisArc();
app.MapOpenApi();
app.MapScalarApiReference(options => options.WithDynamicBaseServerUrl());
app.MapWorkbenchProxy(WorkbenchAddress.For(app.Services));

// A model authored on Studio's canvas declares no screens - the canvas cannot record one - so the scene it
// translates to is empty. Rather than serving a frontend that says the model has nothing to show, the screens
// the model implies are synthesized from its slices, and the routes Arc registered for their commands and
// queries are attached so the frontend calls the real application rather than a guessed URL.
var synthesized = SceneSynthesizer.Synthesize(scene!, model);
if (!ReferenceEquals(synthesized, scene))
{
    StageLog.SynthesizedScreens(app.Logger, synthesized.Screens.Count);
}

// Resolved on first read, not here: Arc registers the modeled commands and queries as endpoints while the
// application starts, so asking for them during configuration finds an empty endpoint set and every element
// ends up without the route it is backed by.
var sceneRoutes = new StageSceneRoutes(synthesized, app.Services, app.Logger);
var stageStrings = new StageStrings(modelPath!);

app.MapGet("/stage/status", () => new StageStatus("ready", new StageStatusModel(model.Name), WarmStageHandoff.ReadHandoffId(modelPath!)));

// A stage that already runs an application cannot take another one. Leaving the route unmapped answered that
// with 405 Method Not Allowed, which reads as a broken endpoint rather than an occupied stage - and the pool
// looking for somewhere to put a play session treated it as a fault instead of moving on to the next stage.
app.MapPost("/stage/load", () => Results.Conflict());
app.MapGet("/stage/scene", () => Results.Json(sceneRoutes.Scene, StageJson.Options));

// What an interaction needs that an element does not: where a command named anywhere in the model is posted.
// Served alongside the scene rather than folded into it, because it answers a question about the running
// application rather than describing what the document says.
app.MapGet("/stage/routes", () => Results.Json(
    new StageRoutes(sceneRoutes.CommandRoutes, sceneRoutes.QueryRoutes),
    StageJson.Options));

// The locales at least one .strings file next to the model declares, and the merged dictionary for one
// of them - a frontend fetches the list to offer a switcher, then a dictionary each time the active
// locale changes, resolving $strings.<key> references at render time rather than compiling them in.
app.MapGet("/stage/locales", () => Results.Json(stageStrings.Locales(), StageJson.Options));
app.MapGet("/stage/strings/{locale}", (string locale) => Results.Json(stageStrings.Dictionary(locale), StageJson.Options));
app.MapFallbackToFile("index.html");
app.Lifetime.ApplicationStarted.Register(() =>
    _ = StageRuntimeRegistrar.RegisterAsync(app.Services, eventStore, model, app.Logger));

await app.RunAsync();

static async Task ResetKernel(IChronicleClient chronicleClient, string eventStoreName)
{
    var eventStore = await chronicleClient.GetEventStore(eventStoreName);
    var services = ((Cratis.Chronicle.Contracts.IChronicleServicesAccessor)eventStore.Connection).Services;
    await services.Server.ResetKernelState();
}
