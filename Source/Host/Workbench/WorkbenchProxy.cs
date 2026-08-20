// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Net.Security;
using Yarp.ReverseProxy.Forwarder;

namespace Cratis.Stage.Host.Workbench;

/// <summary>
/// Serves the Chronicle Workbench of the play session's own bundled kernel under <c>/workbench</c>.
/// </summary>
/// <remarks>
/// The kernel serves the Workbench at the root of its own port, which nothing outside the container can reach -
/// and even reachable, its assets and API calls are written root-absolute, so hanging it off a prefix breaks it.
/// This forwards the prefix to the kernel with the prefix removed, and tells the SPA where it really lives by
/// rewriting the <c>base-path</c> meta tag and the root-absolute asset references in <c>index.html</c> on the way
/// back. Arc's client takes that value as its API base path and prefixes every command, query, SSE and WebSocket
/// URL with it, so the whole surface follows from the one rewrite.
/// <para>
/// The prefix is resolved per request rather than configured, because the Stage does not know it: reached
/// directly it is <c>/workbench</c>, and behind a caller's reverse proxy it is whatever path that proxy states in
/// <c>X-Forwarded-Prefix</c> - which the host already turns into <see cref="HttpRequest.PathBase"/>.
/// </para>
/// </remarks>
public static class WorkbenchProxy
{
    /// <summary>
    /// The path the Workbench is served under.
    /// </summary>
    public const string RoutePrefix = "/workbench";

    const string BasePathMetaPattern = "name=\"base-path\" content=\"\"";

#pragma warning disable CA5359 // Do not disable certificate validation
    // The kernel is in this container, on loopback, presenting the self-signed certificate it generated for
    // itself at startup. There is no chain that could validate and no network segment for anyone to sit on -
    // the connection never leaves the container. The kernel is on TLS at all because it multiplexes gRPC and
    // HTTP/1.1 onto one port and ALPN is what separates them, not because anything here is being protected.
    static readonly HttpMessageInvoker _invoker = new(new SocketsHttpHandler
    {
        UseProxy = false,
        AllowAutoRedirect = false,
        AutomaticDecompression = DecompressionMethods.None,
        UseCookies = false,
        ConnectTimeout = TimeSpan.FromSeconds(5),
        SslOptions = new SslClientAuthenticationOptions
        {
            RemoteCertificateValidationCallback = (_, _, _, _) => true
        }
    });
#pragma warning restore CA5359

    /// <summary>
    /// Adds the services the Workbench proxy needs.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add to.</param>
    /// <returns>The same <see cref="IServiceCollection"/> for chaining.</returns>
    public static IServiceCollection AddWorkbenchProxy(this IServiceCollection services) => services.AddHttpForwarder();

    /// <summary>
    /// Maps the Workbench under <see cref="RoutePrefix"/>, forwarding to the given Chronicle kernel.
    /// </summary>
    /// <param name="app">The <see cref="WebApplication"/> to map on.</param>
    /// <param name="kernelAddress">The address of the Chronicle kernel serving the Workbench.</param>
    /// <returns>The same <see cref="WebApplication"/> for chaining.</returns>
    public static WebApplication MapWorkbenchProxy(this WebApplication app, Uri kernelAddress)
    {
        var destination = kernelAddress.ToString().TrimEnd('/');

        app.MapMethods($"{RoutePrefix}/{{**path}}", ["GET", "POST", "PUT", "DELETE", "PATCH", "HEAD", "OPTIONS"], Forward)
            .AllowAnonymous();

        // Without this the bare prefix 404s rather than serving the Workbench, because ** matches no segment.
        app.MapGet(RoutePrefix, Forward).AllowAnonymous();

        return app;

        async Task Forward(HttpContext context)
        {
            var forwarder = context.RequestServices.GetRequiredService<IHttpForwarder>();
            var publicPrefix = $"{context.Request.PathBase}{RoutePrefix}";
            var transformer = new WorkbenchTransformer(RoutePrefix, publicPrefix, BasePathMetaPattern);

            var error = await forwarder.SendAsync(context, destination, _invoker, ForwarderRequestConfig.Empty, transformer);

            if (error != ForwarderError.None && !context.Response.HasStarted)
            {
                // The kernel is still starting, or has gone away. Say so plainly rather than letting it surface
                // as a generic proxy failure with no indication of which hop failed.
                context.Response.StatusCode = StatusCodes.Status502BadGateway;
            }
        }
    }
}
