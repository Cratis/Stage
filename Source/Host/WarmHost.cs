// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle;
using Cratis.Stage.Runtime;
using Microsoft.AspNetCore.HttpOverrides;

namespace Cratis.Stage.Host;

/// <summary>
/// Runs a Stage that waits, warm, to be handed the model it should run.
/// </summary>
/// <remarks>
/// The warm Stage only accepts the handoff: it writes the handed model to the model directory, resets the kernel and
/// exits with <see cref="HandoffExitCode"/> so the supervisor restarts Stage against the model while Chronicle stays warm.
/// </remarks>
internal static class WarmHost
{
    /// <summary>
    /// The exit code that tells the supervisor a model was handed over and Stage should restart against it.
    /// </summary>
    internal const int HandoffExitCode = 42;

    internal const string ModelDirectory = "/eventmodel";

    internal static async Task Run(string[] args, StageRuntimeEngine engine)
    {
        var eventStore = ContainerEventStoreName.Resolve();
        var builder = WebApplication.CreateBuilder(args);

        builder.Configuration.AddJsonFile(
            Environment.GetEnvironmentVariable("STAGE_CONFIG") is { Length: > 0 } configuredPath
                ? configuredPath
                : Path.Combine(builder.Environment.ContentRootPath, "cratis-stage.json"),
            optional: true,
            reloadOnChange: true);

        var routeOptions = StageHttpRouteOptions.FromConfiguration(builder.Configuration);
        builder.AddStageCratis(eventStore, programIdentifier: $"Cratis Stage ({eventStore})", routeOptions: routeOptions);
        builder.AddStageOrleans();
        builder.Services.AddSingleton<StageEventStoreName>(eventStore);
        builder.Services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedHost | ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedPrefix;
            options.KnownIPNetworks.Clear();
            options.KnownProxies.Clear();
        });

        var app = builder.Build();
        if (engine == StageRuntimeEngine.EventModel)
        {
            StageRuntimeEngineLogging.Deprecated(app.Logger, StageRuntimeEngineSelection.Name(engine));
        }
        app.UseForwardedHeaders();
        app.UseRouting();
        app.UseCratisChronicle();

        using var handoff = new WarmStageHandoff(ModelDirectory);

        app.MapGet("/stage/status", handoff.GetStatus);
        app.MapPost("/stage/load", async (StageLoadRequest request, IChronicleClient chronicleClient, CancellationToken cancellationToken) => await Load(
            handoff,
            request,
            _ => ResetKernel(chronicleClient, eventStore),
            () => RestartAgainstTheModel(app),
            cancellationToken));

        await app.RunAsync();
    }

    /// <summary>
    /// Hands a model to the warm Stage and answers with what became of the handoff.
    /// </summary>
    /// <param name="handoff">The warm Stage taking the handoff.</param>
    /// <param name="request">The handed model.</param>
    /// <param name="resetKernel">Resets the kernel once the model is written.</param>
    /// <param name="accepted">Called once the handoff is accepted, to restart Stage against the model.</param>
    /// <param name="cancellationToken">Cancels the handoff.</param>
    /// <returns><c language="text">202 Accepted</c>, <c language="text">409 Conflict</c> when the Stage was already claimed, or <c language="text">400 Bad Request</c> for a file outside the model directory.</returns>
    internal static async Task<IResult> Load(
        WarmStageHandoff handoff,
        StageLoadRequest request,
        Func<CancellationToken, Task> resetKernel,
        Action accepted,
        CancellationToken cancellationToken)
    {
        var result = await handoff.Load(request, resetKernel, cancellationToken);

        if (result == StageHandoffResult.Conflict)
        {
            return Results.Conflict();
        }

        if (result == StageHandoffResult.InvalidPath)
        {
            return Results.BadRequest();
        }

        accepted();
        return Results.Accepted();
    }

    static void RestartAgainstTheModel(WebApplication app)
    {
        Environment.ExitCode = HandoffExitCode;
        _ = Task.Run(async () =>
        {
            await Task.Delay(TimeSpan.FromMilliseconds(100));
            app.Lifetime.StopApplication();
        });
    }

    static async Task ResetKernel(IChronicleClient chronicleClient, string eventStoreName)
    {
        var eventStore = await chronicleClient.GetEventStore(eventStoreName);
        var services = ((Cratis.Chronicle.Contracts.IChronicleServicesAccessor)eventStore.Connection).Services;
        await services.Server.ResetKernelState();
    }
}
