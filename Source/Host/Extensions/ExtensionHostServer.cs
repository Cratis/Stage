// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Stage.Host.Invocations;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Options;

namespace Cratis.Stage.Host.Extensions;

/// <summary>
/// Serves Stage's host services on a listener of their own. The services speak gRPC, which needs HTTP/2 end to end, so they do not share the port, the
/// protocols or the pipeline of the application: an extension reaches nothing but these services here.
/// </summary>
/// <param name="services">The services of the host, whose singletons the host services use.</param>
/// <param name="options">The <see cref="ExtensionOptions"/>.</param>
/// <param name="logger">The logger.</param>
public sealed class ExtensionHostServer(IServiceProvider services, IOptions<ExtensionOptions> options, ILogger<ExtensionHostServer> logger) : IHostedService
{
    WebApplication? _app;

    /// <summary>
    /// Gets the port the host services listen on once started. It differs from the configured one when that is port zero in a specification.
    /// </summary>
    public int Port { get; private set; }

    /// <inheritdoc/>
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var configured = options.Value;
        var builder = WebApplication.CreateEmptyBuilder(new WebApplicationOptions());
        builder.WebHost.UseKestrelCore().ConfigureKestrel(kestrel =>
            kestrel.Listen(IPAddress.Parse(configured.HostAddress), configured.HostPort, listen => listen.Protocols = HttpProtocols.Http2));

        builder.Services.AddSingleton(services.GetRequiredService<ILoggerFactory>());
        builder.Services.AddSingleton(typeof(ILogger<>), typeof(Logger<>));
        builder.Services.AddSingleton(services.GetRequiredService<ExtensionSessions>());
        builder.Services.AddSingleton(services.GetRequiredService<ISystemSettings>());
        builder.Services.AddSingleton(services.GetRequiredService<IContractTriggers>());
        builder.Services.AddSingleton(services.GetRequiredService<IAbilityInvocations>());
        builder.Services.AddSingleton(services.GetRequiredService<ExtensionTelemetry>());
        builder.Services.AddRouting();
        builder.Services.AddGrpc();

        var app = builder.Build();
        app.UseRouting();
        app.MapGrpcService<StageHostService>();
        await app.StartAsync(cancellationToken);
        _app = app;
        Port = new Uri(app.Urls.FirstOrDefault() ?? $"http://{configured.HostAddress}:{configured.HostPort}").Port;
        ExtensionLog.Listening(logger, $"{configured.HostAddress}:{Port}");
    }

    /// <inheritdoc/>
    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_app is null) return;
        await _app.StopAsync(cancellationToken);
        await _app.DisposeAsync();
        _app = null;
    }
}
