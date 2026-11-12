// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Stage.Host.Extensions.Activation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Server.Kestrel.Core;

namespace Cratis.Stage.Host.Extensions.given;

/// <summary>
/// An <see cref="IBackendRuntime"/> that starts a backend that speaks the extension protocol in the spec's own process, at the address
/// the activator chose, and counts how many times it was asked to.
/// </summary>
/// <param name="backend">The <see cref="FakeBackend"/> every start serves.</param>
public sealed class InProcessBackendRuntime(FakeBackend backend) : IBackendRuntime
{
    /// <summary>
    /// Gets the launches, which is how often the host started the backend.
    /// </summary>
    public List<BackendLaunch> Starts { get; } = [];

    /// <inheritdoc/>
    public ExecutionTier Tier => ExecutionTier.SharedHost;

    /// <inheritdoc/>
    public async Task<StartedBackend> Start(BackendLaunch launch, CancellationToken cancellationToken)
    {
        Starts.Add(launch);
        var builder = WebApplication.CreateEmptyBuilder(new WebApplicationOptions());
        builder.WebHost.UseKestrelCore().ConfigureKestrel(kestrel => kestrel.Listen(IPAddress.Loopback, launch.Address.Port, listen => listen.Protocols = HttpProtocols.Http2));
        builder.Services.AddSingleton(backend);
        builder.Services.AddRouting();
        builder.Services.AddGrpc();
        var app = builder.Build();
        app.UseRouting();
        app.MapGrpcService<FakeBackend>();
        await app.StartAsync(cancellationToken);
        return new Running(app);
    }

    sealed class Running(WebApplication app) : StartedBackend
    {
        readonly TaskCompletionSource<string> _exited = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public override Task<string> Exited => _exited.Task;

        public override async ValueTask DisposeAsync()
        {
            _exited.TrySetResult("stopped");
            await app.StopAsync();
            await app.DisposeAsync();
        }
    }
}
