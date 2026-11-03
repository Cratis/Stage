// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Net.Sockets;
using Cratis.Orleans.Chronicle.StateMachines;
using Microsoft.Extensions.Options;
using Orleans.Configuration;
using Xunit;

namespace Cratis.Stage.Host.for_StageOrleansConfiguration;

public class when_configuring_the_stage_silo
{
    [Fact]
    public void should_leave_the_ports_the_chronicle_kernel_binds_to_the_kernel()
    {
        var builder = WebApplication.CreateBuilder();
        builder.AddStageOrleans();
        using var app = builder.Build();

        var endpoints = app.Services.GetRequiredService<IOptions<EndpointOptions>>().Value;
        var cluster = app.Services.GetRequiredService<IOptions<ClusterOptions>>().Value;
        var membership = app.Services.GetRequiredService<IOptions<DevelopmentClusterMembershipOptions>>().Value;

        Assert.Equal(StageOrleansConfiguration.DefaultSiloPort, endpoints.SiloPort);
        Assert.Equal(StageOrleansConfiguration.DefaultGatewayPort, endpoints.GatewayPort);
        Assert.NotEqual(11111, endpoints.SiloPort);
        Assert.NotEqual(30000, endpoints.GatewayPort);
        Assert.Equal(StageOrleansConfiguration.ClusterName, cluster.ClusterId);
        Assert.Equal(endpoints.SiloPort, membership.PrimarySiloEndpoint?.Port);
    }

    [Fact]
    public void should_take_the_ports_from_configuration()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Configuration["Stage:Orleans:SiloPort"] = "22222";
        builder.Configuration["Stage:Orleans:GatewayPort"] = "32000";
        builder.AddStageOrleans();
        using var app = builder.Build();

        var endpoints = app.Services.GetRequiredService<IOptions<EndpointOptions>>().Value;
        Assert.Equal(22222, endpoints.SiloPort);
        Assert.Equal(32000, endpoints.GatewayPort);
    }

    [Fact]
    public void should_register_event_sourced_state_machines()
    {
        var builder = WebApplication.CreateBuilder();
        builder.AddStageOrleans();
        using var app = builder.Build();

        Assert.NotNull(app.Services.GetService<IEventSourcedStateMachineDefinitions>());
    }

    [Fact]
    public async Task should_start_without_a_database()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Configuration["Stage:Orleans:SiloPort"] = FreePort().ToString();
        builder.Configuration["Stage:Orleans:GatewayPort"] = FreePort().ToString();
        builder.Logging.ClearProviders();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.AddStageOrleans();
        await using var app = builder.Build();

        await app.StartAsync();
        Assert.NotNull(app.Services.GetService<IGrainFactory>());
        await app.StopAsync();
    }

    static int FreePort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }
}
