// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using Cratis.Chronicle;
using Cratis.Stage.Api;
using Cratis.Stage.Contracts;
using Cratis.Stage.Host;
using Cratis.Stage.Naming;
using Cratis.Stage.Runtime;
using Scalar.AspNetCore;

// Force invariant culture for the Backend
CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;
CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;

var modelPath = args.FirstOrDefault(argument => !argument.StartsWith('-'))
    ?? throw new MissingModelArgument();

var model = await EventModelLoader.LoadAsync(modelPath);
var eventStore = DockerStyleName.Generate();

var builder = WebApplication.CreateBuilder(args);

// Deployment configuration comes from a dedicated cratis-stage.json file (path overridable through the
// STAGE_CONFIG environment variable) — not appsettings.json. appsettings.json only carries hosting defaults.
builder.Configuration.AddJsonFile(
    Environment.GetEnvironmentVariable("STAGE_CONFIG") is { Length: > 0 } configuredPath
        ? configuredPath
        : Path.Combine(builder.Environment.ContentRootPath, "cratis-stage.json"),
    optional: true,
    reloadOnChange: true);

builder.AddStageCratis(eventStore, programIdentifier: $"Cratis Stage ({eventStore})");

builder.Services.AddSingleton(model);
builder.Services.AddSingleton<DynamicTypeFactory>();
builder.Services.AddControllers();

// Add OpenAPI and Scalar — filter out framework infrastructure operations so only the engine's own
// model operations are shown.
builder.Services.AddOpenApi(options => options.AddDocumentTransformer<StageOnlyOperationsDocumentTransformer>());

var app = builder.Build();

StageLog.Running(app.Logger, model.Name, eventStore);

app.UseRouting();
app.UseWebSockets();
app.MapControllers();
app.UseCratisArc();
app.UseCratisChronicle();

// Map OpenAPI endpoint and configure Scalar
app.MapOpenApi();
app.MapScalarApiReference();

// Once the app has started (and UseCratisChronicle has connected the client), register the model's read models
// and projections with Chronicle from the runtime model data so projections run and populate the read-model store.
app.Lifetime.ApplicationStarted.Register(() =>
    _ = StageRuntimeRegistrar.RegisterAsync(app.Services, eventStore, model, app.Logger));

await app.RunAsync();
