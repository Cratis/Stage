// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using Cratis.Chronicle;
using Cratis.Stage.Api;
using Cratis.Stage.Contracts;
using Cratis.Stage.Host;
using Cratis.Stage.Host.Workbench;
using Cratis.Stage.Naming;
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

var stageApplication = warmMode ? null : await EventModelLoader.LoadStageApplicationFromDirectoryAsync(modelPath!);
var model = stageApplication?.EventModel;
var scene = stageApplication?.Scene;
var eventStore = DockerStyleName.Generate();
var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile(
    Environment.GetEnvironmentVariable("STAGE_CONFIG") is { Length: > 0 } configuredPath
        ? configuredPath
        : Path.Combine(builder.Environment.ContentRootPath, "cratis-stage.json"),
    optional: true,
    reloadOnChange: true);

builder.AddStageCratis(eventStore, programIdentifier: $"Cratis Stage ({eventStore})");

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
    app.UseStaticFiles();
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
app.UseCratisArc();
app.MapOpenApi();
app.MapScalarApiReference(options => options.WithDynamicBaseServerUrl());
app.MapWorkbenchProxy(WorkbenchAddress.For(app.Services));
app.MapGet("/stage/status", () => new StageStatus("ready", new StageStatusModel(model.Name), WarmStageHandoff.ReadHandoffId(modelPath!)));
app.MapGet("/stage/scene", () => Results.Json(scene, StageJson.Options));
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
