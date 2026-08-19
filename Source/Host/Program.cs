// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using Cratis.Chronicle;
using Cratis.Stage.Api;
using Cratis.Stage.Contracts;
using Cratis.Stage.Host;
using Cratis.Stage.Naming;
using Cratis.Stage.Runtime;
using Microsoft.AspNetCore.HttpOverrides;
using Scalar.AspNetCore;

// Force invariant culture for the Backend
CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;
CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;

var modelPath = args.FirstOrDefault(argument => !argument.StartsWith('-'))
    ?? throw new MissingModelArgument();

var model = await EventModelLoader.LoadFromDirectoryAsync(modelPath);
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
builder.Services.AddSingleton<StageEventStoreName>(eventStore);
builder.Services.AddSingleton<IAppendProducedEvents, ProducedEventAppender>();
builder.Services.AddSingleton<IProvideStageIdentity, StageIdentity>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddControllers();

// A play session is reached through its caller's reverse proxy on a path prefix (Studio proxies
// https://<studio>/api/play/<session>/… to this container's root), and the proxy sends the standard
// X-Forwarded-Host, X-Forwarded-Proto and X-Forwarded-Prefix headers. Honoring them is what lets anything
// that derives a URL from the request - the OpenAPI document's servers entry, and through it Scalar's
// "Try it" - target the public address rather than the internal service name. The known networks and proxies
// are cleared because there is no fixed proxy to trust: the Stage is a disposable sandbox that only ever sits
// behind the caller's own proxy.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedHost | ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedPrefix;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

// Add OpenAPI and Scalar — filter out framework infrastructure operations so only the engine's own
// model operations are shown.
builder.Services.AddOpenApi(options => options.AddDocumentTransformer<StageOnlyOperationsDocumentTransformer>());

var app = builder.Build();

StageLog.Running(app.Logger, model.Name, eventStore);

// First in the pipeline so every later component sees the public host, scheme and path base.
app.UseForwardedHeaders();
app.UseRouting();
app.UseWebSockets();
app.MapControllers();
app.UseCratisArc();
app.UseCratisChronicle();

// Map OpenAPI endpoint and configure Scalar. The base server URL is resolved from the page's own origin and
// path base, so the reference works the same when served through a path-prefixed proxy as when served directly.
app.MapOpenApi();
app.MapScalarApiReference(options => options.WithDynamicBaseServerUrl());

// Once the app has started (and UseCratisChronicle has connected the client), register the model's read models
// and projections with Chronicle from the runtime model data so projections run and populate the read-model store.
app.Lifetime.ApplicationStarted.Register(() =>
    _ = StageRuntimeRegistrar.RegisterAsync(app.Services, eventStore, model, app.Logger));

await app.RunAsync();
