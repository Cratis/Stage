// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Choreography.Chronicle.Hosting;
using Cratis.Choreography.Hosting;
using Cratis.Stage.Host.Extensions;
using Cratis.Stage.Host.Invocations;
using Orleans.Configuration;
using Orleans.Hosting;

namespace Cratis.Stage.Host;

/// <summary>
/// Extension methods for hosting the Orleans silo that runs jobs, event-sourced state machines and workflows in the Stage host.
/// </summary>
public static class StageOrleansConfiguration
{
    /// <summary>
    /// The default silo-to-silo port. It differs from the Orleans default (11111) the co-hosted Chronicle kernel binds in the same container.
    /// </summary>
    public const int DefaultSiloPort = 21111;

    /// <summary>
    /// The default gateway port. It differs from the Orleans default (30000) the co-hosted Chronicle kernel binds in the same container.
    /// </summary>
    public const int DefaultGatewayPort = 31000;

    /// <summary>
    /// The cluster and service id of the Stage silo, distinct from the Chronicle kernel's own cluster.
    /// </summary>
    public const string ClusterName = "cratis-stage";

    /// <summary>
    /// Hosts an Orleans silo in the Stage runtime with the local configuration: localhost clustering and in-memory grain storage,
    /// so it starts without MongoDB. Jobs, Chronicle-backed event-sourced state machines and workflows are registered.
    /// </summary>
    /// <param name="builder">The <see cref="WebApplicationBuilder"/> to configure.</param>
    /// <returns>The same <see cref="WebApplicationBuilder"/> for chaining.</returns>
    /// <remarks>
    /// The Chronicle kernel runs in the same container and is itself an Orleans silo on the Orleans default ports. The Stage silo
    /// therefore listens on its own ports, which <c language="text">Stage:Orleans:SiloPort</c> and <c language="text">Stage:Orleans:GatewayPort</c> can override.
    /// </remarks>
    public static WebApplicationBuilder AddStageOrleans(this WebApplicationBuilder builder)
    {
        var siloPort = builder.Configuration.GetValue("Stage:Orleans:SiloPort", DefaultSiloPort);
        var gatewayPort = builder.Configuration.GetValue("Stage:Orleans:GatewayPort", DefaultGatewayPort);

        var local = CratisChoreographyOptions.Local();
        var invocations = builder.Configuration.GetSection(StageInvocationOptions.Section).Get<StageInvocationOptions>() ?? new();
        builder.AddCratisChoreography(new CratisChoreographyOptions
        {
            Clustering = local.Clustering,
            Storage = builder.Configuration.GetValue("Stage:Orleans:Storage", local.Storage),
            ConnectionString = builder.Configuration.GetValue("Stage:Orleans:ConnectionString", local.ConnectionString),
            DatabaseName = builder.Configuration.GetValue("Stage:Orleans:DatabaseName", local.DatabaseName),
            ClusterId = ClusterName,
            ServiceId = ClusterName,
            Workflows = true,
            ConfigureWorkflows = options => SystemGates.Concurrency(invocations, options.CapabilityLimits)
        });

        // Applied after the localhost clustering the silo hosting registered, so these endpoints win. The development
        // membership's primary silo must be this silo, or it waits for a silo that is not there.
        builder.Services.Configure<EndpointOptions>(options =>
        {
            options.SiloPort = siloPort;
            options.GatewayPort = gatewayPort;
        });
        builder.Services.Configure<DevelopmentClusterMembershipOptions>(options =>
            options.PrimarySiloEndpoint = new IPEndPoint(IPAddress.Loopback, siloPort));

        builder.Services.AddEventSourcedStateMachines();
        builder.Services.AddEventSourcedWorkflows();

        // The engine instantiates every definition source and performer source it finds that is not registered, so wherever it is hosted the invocation
        // services must be registered too. They come after the lifecycles, which register their own before this, so abilities are found beside them.
        builder.Services.AddStageInvocations(builder.Configuration).ComposeWithAbilities().AddStageExtensions(builder.Configuration);
        return builder;
    }
}
