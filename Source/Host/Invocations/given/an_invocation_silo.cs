// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Net.Sockets;
using Cratis.Choreography.Hosting;
using Cratis.Choreography.Workflows.Adapters;
using Cratis.Specifications;
using Orleans.Configuration;
using Orleans.Hosting;
using Xunit;

namespace Cratis.Stage.Host.Invocations.given;

/// <summary>
/// An in-process silo that runs ability invocations through the workflow engine, with a scripted extension backend.
/// </summary>
public abstract class an_invocation_silo : Specification
{
    protected WebApplication _app = null!;
    protected ScriptedBackend _backend = null!;
    protected RecordingHandler _reactions = null!;
    protected RecordingHandler _stateMachines = null!;
    protected RecordingHandler _workflows = null!;

    protected IAbilityInvocations Invocations => _app.Services.GetRequiredService<IAbilityInvocations>();

    protected IInvocationLedger Ledger => _app.Services.GetRequiredService<IInvocationLedger>();

    protected abstract IEnumerable<AbilityDescriptor> Abilities { get; }

    protected virtual bool MapEndpoints => false;

    protected static InvocationOrigin Origin(string causation, InvocationOriginKind kind = InvocationOriginKind.ReactionBranch) =>
        new(kind, "Fulfillment/PostToWarehouse/branch 1", causation);

    protected static AbilityDescriptor Describe(string name, EffectClass effect, params string[] outcomes) =>
        new(new("Slack", name, 1), effect, [.. outcomes], "sha256:aaaa") { MaxAttempts = 3, RetryDelay = TimeSpan.FromMilliseconds(100), Timeout = TimeSpan.FromSeconds(10) };

    protected static async Task Eventually(Func<Task<bool>> condition, string expectation)
    {
        var deadline = DateTimeOffset.UtcNow.AddSeconds(45);
        while (!await condition())
        {
            Assert.True(DateTimeOffset.UtcNow < deadline, $"Timed out waiting for {expectation}.");
            await Task.Delay(50);
        }
    }

    protected Task Eventually(Func<bool> condition, string expectation) => Eventually(() => Task.FromResult(condition()), expectation);

    protected async Task Destroy()
    {
        await _app.StopAsync();
        await _app.DisposeAsync();
    }

    protected virtual void Configure(WebApplicationBuilder builder)
    {
    }

    static int FreePort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    void Establish()
    {
        _backend = new();
        _reactions = new(InvocationOriginKind.ReactionBranch);
        _stateMachines = new(InvocationOriginKind.StateMachineEntry);
        _workflows = new(InvocationOriginKind.WorkflowAction);
        var builder = WebApplication.CreateBuilder();
        builder.Logging.ClearProviders();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Services.AddSingleton<IAbilityCatalog>(new AbilityCatalog(Abilities));
        builder.Services.AddSingleton<IAbilityBackend>(_backend);
        builder.Services.AddSingleton<IInvocationOriginHandler>(_reactions);
        builder.Services.AddSingleton<IInvocationOriginHandler>(_stateMachines);
        builder.Services.AddSingleton<IInvocationOriginHandler>(_workflows);
        builder.Services.AddAuthorizationBuilder().AddPolicy("Managers", policy => policy.RequireRole("manager"));
        builder.Configuration["Stage:Invocations:AllowVolatileOnce"] = "true";
        Configure(builder);
        builder.Services.AddStageInvocations(builder.Configuration);
        builder.Services.AddSingleton<IWorkflowNodePerformers>(provider => provider.GetRequiredService<AbilityWorkflowPerformers>());
        var siloPort = FreePort();
        builder.AddCratisChoreography(new CratisChoreographyOptions
        {
            Clustering = ClusteringMode.Localhost,
            Storage = GrainStorageMode.InMemory,
            ClusterId = "invocation-spec",
            ServiceId = "invocation-spec",
            Workflows = true
        });
        builder.Services.Configure<EndpointOptions>(options =>
        {
            options.SiloPort = siloPort;
            options.GatewayPort = FreePort();
        });
        builder.Services.Configure<DevelopmentClusterMembershipOptions>(options => options.PrimarySiloEndpoint = new IPEndPoint(IPAddress.Loopback, siloPort));
        _app = builder.Build();
        if (MapEndpoints) InvocationEndpoints.Map(_app);
        _app.StartAsync().GetAwaiter().GetResult();
    }
}
