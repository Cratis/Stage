// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Net.Sockets;
using Cratis.Choreography.Hosting;
using Cratis.Specifications;
using Cratis.Stage.Host.Extensions.Inbound;
using Microsoft.Extensions.Options;
using Orleans.Configuration;
using Orleans.Hosting;
using Xunit;

namespace Cratis.Stage.Host.Extensions.for_GrainInboundDeliveries;

public class when_the_silo_restarts_activations : Specification
{
    WebApplication _app = null!;
    InboundReceipt _first = null!;
    InboundReceipt _replay = null!;
    InboundDelivery? _afterwards;
    IReadOnlyList<InboundDelivery> _due = null!;
    InboundDelivery _settled = null!;
    IReadOnlyList<InboundDelivery> _dueAfterwards = null!;
    int _open;

    async Task Establish()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Logging.ClearProviders();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Services.AddSingleton(Options.Create(new ExtensionOptions()));
        builder.Services.AddSingleton(TimeProvider.System);
        builder.Services.AddSingleton<IInboundDeliveries, GrainInboundDeliveries>();
        var siloPort = FreePort();
        builder.AddCratisChoreography(new CratisChoreographyOptions
        {
            Clustering = ClusteringMode.Localhost,
            Storage = GrainStorageMode.InMemory,
            ClusterId = "inbound-spec",
            ServiceId = "inbound-spec",
            Workflows = true
        });
        builder.Services.Configure<EndpointOptions>(options =>
        {
            options.SiloPort = siloPort;
            options.GatewayPort = FreePort();
        });
        builder.Services.Configure<DevelopmentClusterMembershipOptions>(options => options.PrimarySiloEndpoint = new IPEndPoint(IPAddress.Loopback, siloPort));
        _app = builder.Build();
        await _app.StartAsync();
    }

    async Task Because()
    {
        var deliveries = _app.Services.GetRequiredService<IInboundDeliveries>();
        var id = InboundDeliveryRules.Identify("Payments", "POST", "/webhook", string.Empty, [1, 2, 3]);
        var request = new InboundDelivery(id, "Payments", "POST", "/webhook", string.Empty, [new("X-Signature", ["abc"])], [1, 2, 3], DateTimeOffset.UtcNow);
        _first = await deliveries.Receive(request);

        await _app.Services.GetRequiredService<IGrainFactory>().GetGrain<Orleans.Runtime.IManagementGrain>(0).ForceActivationCollection(TimeSpan.Zero);

        _afterwards = await deliveries.Get(id);
        _replay = await deliveries.Receive(request with { Received = DateTimeOffset.UtcNow });
        _due = await deliveries.Due(DateTimeOffset.UtcNow.AddSeconds(1));
        _open = await deliveries.CountOpen();
        _settled = await deliveries.Settle(id, InboundOutcome.Delivered, 200, "ok");
        _dueAfterwards = await deliveries.Due(DateTimeOffset.UtcNow.AddSeconds(1));
    }

    async Task Destroy() => await _app.DisposeAsync();

    static int FreePort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    [Fact] void should_store_a_new_request() => (!_first.Replay && _first.Delivery.Status == InboundStatus.Pending).ShouldBeTrue();
    [Fact] void should_keep_the_raw_request_across_a_restart() => (_afterwards?.Body.SequenceEqual(new byte[] { 1, 2, 3 }) == true && _afterwards.Headers[0].Values[0] == "abc").ShouldBeTrue();
    [Fact] void should_recognise_a_replay_after_the_restart() => (_replay.Replay && _replay.Delivery.Replays == 1).ShouldBeTrue();
    [Fact] void should_still_have_the_request_due_after_the_restart() => _due.Count.ShouldEqual(1);
    [Fact] void should_count_it_as_waiting_once() => _open.ShouldEqual(1);
    [Fact] void should_settle_it() => _settled.Status.ShouldEqual(InboundStatus.Delivered);
    [Fact] void should_stop_listing_it_as_due_once_delivered() => _dueAfterwards.ShouldBeEmpty();
}
