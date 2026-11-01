// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Orleans.Chronicle.Hosting;
using Cratis.Orleans.Hosting;
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
    /// therefore listens on its own ports, which <c>Stage:Orleans:SiloPort</c> and <c>Stage:Orleans:GatewayPort</c> can override.
    /// </remarks>
    public static WebApplicationBuilder AddStageOrleans(this WebApplicationBuilder builder)
    {
        var siloPort = builder.Configuration.GetValue("Stage:Orleans:SiloPort", DefaultSiloPort);
        var gatewayPort = builder.Configuration.GetValue("Stage:Orleans:GatewayPort", DefaultGatewayPort);

        var local = CratisOrleansOptions.Local();
        builder.AddCratisOrleans(new CratisOrleansOptions
        {
            Clustering = local.Clustering,
            Storage = local.Storage,
            ClusterId = ClusterName,
            ServiceId = ClusterName,
            Workflows = true
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
        return builder;
    }
}
