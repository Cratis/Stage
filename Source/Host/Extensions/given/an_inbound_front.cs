// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Stage.Host.Extensions.Inbound;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Options;

namespace Cratis.Stage.Host.Extensions.given;

/// <summary>
/// A running extension backend, Stage's inbound endpoint in front of it and the dispatcher that delivers what the endpoint stores.
/// </summary>
public class an_inbound_front : an_extension_protocol
{
    protected WebApplication _front = null!;
    protected HttpClient _client = null!;
    protected ManualTime _time = null!;
    protected InMemoryInboundDeliveries _deliveries = null!;
    protected InboundDeliveryDispatcher _dispatcher = null!;

    protected async Task StartFront()
    {
        await StartBackend();
        await StartStage();
        _backend.Description.HttpRoutes.Add("/webhook");

        _time = new ManualTime();
        _deliveries = new InMemoryInboundDeliveries(_time, _options.Inbound);
        _dispatcher = new InboundDeliveryDispatcher(_deliveries, _connections, Options.Create(_options), _time, _logs.CreateLogger<InboundDeliveryDispatcher>());

        var builder = WebApplication.CreateEmptyBuilder(new WebApplicationOptions());
        builder.WebHost.UseKestrelCore().ConfigureKestrel(kestrel => kestrel.Listen(IPAddress.Loopback, 0, listen => listen.Protocols = HttpProtocols.Http1));
        builder.Services.AddSingleton(_connections);
        builder.Services.AddSingleton<IExtensionEndpoints>(new ConfiguredExtensionEndpoints(Options.Create(_options)));
        builder.Services.AddSingleton<IInboundDeliveries>(_deliveries);
        builder.Services.AddSingleton(_dispatcher);
        builder.Services.AddSingleton(Options.Create(_options));
        builder.Services.AddRouting();
        _front = builder.Build();
        _front.UseRouting();
        ExtensionHttpEndpoints.Map(_front);
        InboundDeliveryEndpoints.Map(_front);
        await _front.StartAsync();
        _client = new HttpClient { BaseAddress = new Uri(_front.Urls.First()) };
    }

    protected new async Task Destroy()
    {
        _client.Dispose();
        await _front.StopAsync();
        await _front.DisposeAsync();
    }
}
