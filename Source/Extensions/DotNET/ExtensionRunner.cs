// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Microsoft.AspNetCore.Server.Kestrel.Core;

namespace Cratis.Stage.Extensions;

/// <summary>
/// Runs an <see cref="ExtensionDefinition"/> as a gRPC server Stage connects to.
/// </summary>
public static class ExtensionRunner
{
    /// <summary>
    /// Builds the server without starting it.
    /// </summary>
    /// <param name="definition">The <see cref="ExtensionDefinition"/>.</param>
    /// <param name="address">The address to listen on. Use port 0 to let the operating system choose.</param>
    /// <returns>The application, whose <c lang="text">Urls</c> hold the chosen address once it is started.</returns>
    public static WebApplication Build(ExtensionDefinition definition, IPEndPoint address)
    {
        var builder = WebApplication.CreateEmptyBuilder(new WebApplicationOptions());
        builder.WebHost.UseKestrelCore().ConfigureKestrel(kestrel => kestrel.Listen(address, listen => listen.Protocols = HttpProtocols.Http2));
        builder.Services.AddSingleton(new ExtensionBackendService(definition));
        builder.Services.AddRouting();
        builder.Services.AddGrpc();
        var app = builder.Build();
        app.UseRouting();
        app.MapGrpcService<ExtensionBackendService>();
        return app;
    }

    /// <summary>
    /// Runs the extension until it is cancelled.
    /// </summary>
    /// <param name="definition">The <see cref="ExtensionDefinition"/>.</param>
    /// <param name="address">The address to listen on, which is where Stage's configuration points.</param>
    /// <param name="cancellationToken">Stops the server.</param>
    /// <returns>A task that completes when the server has stopped.</returns>
    public static async Task Run(ExtensionDefinition definition, IPEndPoint address, CancellationToken cancellationToken = default)
    {
        await using var app = Build(definition, address);
        await app.RunAsync(cancellationToken);
    }
}
