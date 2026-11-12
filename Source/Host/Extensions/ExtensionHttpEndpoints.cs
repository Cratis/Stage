// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Frozen;
using Cratis.Stage.Extensions.Protocol;
using Cratis.Stage.Host.Extensions.Activation;
using Cratis.Stage.Host.Extensions.Inbound;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.Extensions.Options;
using RequestContext = Cratis.Stage.Extensions.Protocol.RequestContext;

namespace Cratis.Stage.Host.Extensions;

/// <summary>
/// Receives the inbound HTTP traffic of the extensions, for a webhook or a callback an extension owns. The endpoint is anonymous by design, as a webhook's caller
/// is not a Stage user: the extension verifies the request, with the signature header its provider sends.
/// <para>
/// A request that may change something (<c lang="text">POST</c>, <c lang="text">PUT</c>, <c lang="text">PATCH</c>, <c lang="text">DELETE</c>) is stored with its raw
/// body and headers and acknowledged at once with <c lang="text">202 Accepted</c>; the <see cref="InboundDeliveryDispatcher"/> delivers it, activating the extension
/// when it is not running and retrying when it fails. A request that only reads (<c lang="text">GET</c>, <c lang="text">HEAD</c>, <c lang="text">OPTIONS</c>) is
/// passed to the extension and answered with what it answers, as such a request asks a question, such as a verification challenge.
/// </para>
/// </summary>
public static class ExtensionHttpEndpoints
{
    /// <summary>
    /// Gets the path prefix the requests are routed under: <c lang="text">/extensions/&lt;System&gt;/&lt;path&gt;</c>.
    /// </summary>
    public const string Prefix = "/extensions";

    /// <summary>
    /// Gets the largest body that is forwarded.
    /// </summary>
    public const long MaxBodyBytes = 1_048_576;

    static readonly FrozenSet<string> _reading = new[] { "GET", "HEAD", "OPTIONS" }.ToFrozenSet(StringComparer.OrdinalIgnoreCase);
    static readonly string[] _methods = ["GET", "POST", "PUT", "PATCH", "DELETE", "HEAD", "OPTIONS"];
    static readonly FrozenSet<string> _notForwarded = new[] { "Cookie", "Authorization", "Host", "Connection", "Transfer-Encoding", "Content-Length" }.ToFrozenSet(StringComparer.OrdinalIgnoreCase);
    static readonly FrozenSet<string> _notReturned = new[] { "Connection", "Transfer-Encoding", "Content-Length", "Set-Cookie" }.ToFrozenSet(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Maps the endpoint.
    /// </summary>
    /// <param name="endpoints">The route builder.</param>
    /// <returns>The same builder, for chaining.</returns>
    public static IEndpointRouteBuilder Map(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapMethods($"{Prefix}/{{system}}/{{**path}}", _methods, Handle).ExcludeFromDescription();
        return endpoints;
    }

    internal static bool Matches(string route, string path)
    {
        var prefix = route.StartsWith('/') ? route : "/" + route;
        return path.Equals(prefix, StringComparison.Ordinal) || path.StartsWith(prefix.TrimEnd('/') + "/", StringComparison.Ordinal);
    }

    static async Task<IResult> Handle(
        HttpContext http,
        string system,
        string? path,
        ExtensionConnections connections,
        IExtensionEndpoints endpoints,
        IInboundDeliveries deliveries,
        InboundDeliveryDispatcher dispatcher,
        IOptions<ExtensionOptions> options)
    {
        var relative = "/" + (path ?? string.Empty);
        if (http.Request.ContentLength > MaxBodyBytes) return Results.StatusCode(StatusCodes.Status413PayloadTooLarge);
        if (_reading.Contains(http.Request.Method)) return await Proxy(http, system, relative, connections);

        if (connections.Connected(system) is { } running)
        {
            if (!running.Description.HttpRoutes.Any(route => Matches(route, relative))) return Results.NotFound();
        }
        else if (!await endpoints.Knows(system, http.RequestAborted))
        {
            return Results.NotFound();
        }

        var body = await ReadBody(http);
        if (body is null) return Results.StatusCode(StatusCodes.Status413PayloadTooLarge);
        if (await deliveries.CountOpen() >= options.Value.Inbound.MaxOpen)
        {
            http.Response.Headers.RetryAfter = "60";
            return Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
        }

        var query = http.Request.QueryString.HasValue ? http.Request.QueryString.Value[1..] : string.Empty;
        var headers = http.Request.Headers
            .Where(header => !_notForwarded.Contains(header.Key))
            .Select(header => new InboundHeader(header.Key, [.. header.Value.OfType<string>()]))
            .ToArray();
        var delivery = new InboundDelivery(
            InboundDeliveryRules.Identify(system, http.Request.Method, relative, query, body),
            system,
            http.Request.Method,
            relative,
            query,
            headers,
            body,
            DateTimeOffset.UtcNow);

        var receipt = await deliveries.Receive(delivery);
        if (!receipt.Replay) dispatcher.Wake();
        return Results.Accepted();
    }

    static async Task<byte[]?> ReadBody(HttpContext http)
    {
        await using var body = new MemoryStream();
        var buffer = new byte[8192];
        int read;
        while ((read = await http.Request.Body.ReadAsync(buffer, http.RequestAborted)) > 0)
        {
            if (body.Length + read > MaxBodyBytes) return null;
            await body.WriteAsync(buffer.AsMemory(0, read), http.RequestAborted);
        }

        return body.ToArray();
    }

    static async Task<IResult> Proxy(HttpContext http, string system, string relative, ExtensionConnections connections)
    {
        if ((await connections.Connect(system, http.RequestAborted)).Connection is not { } connection) return Results.NotFound();
        using var inFlight = http.RequestServices.GetService<IBackendActivity>()?.Begin(system);
        if (!connection.Description.HttpRoutes.Any(route => Matches(route, relative))) return Results.NotFound();

        await using var body = new MemoryStream();
        await http.Request.Body.CopyToAsync(body, http.RequestAborted);
        if (body.Length > MaxBodyBytes) return Results.StatusCode(StatusCodes.Status413PayloadTooLarge);

        var request = new InboundHttpRequest
        {
            Context = new RequestContext
            {
                System = system,
                Tenant = Invocations.AbilityInvocations.Scope.Value,
                Traceparent = System.Diagnostics.Activity.Current?.Id ?? string.Empty,
                Deadline = Timestamp.FromDateTime(DateTime.UtcNow.AddSeconds(30))
            },
            Method = http.Request.Method,
            Path = relative,
            Query = http.Request.QueryString.HasValue ? http.Request.QueryString.Value[1..] : string.Empty,
            Body = ByteString.CopyFrom(body.ToArray())
        };
        foreach (var header in http.Request.Headers.Where(header => !_notForwarded.Contains(header.Key)))
        {
            var forwarded = new Header { Name = header.Key };
            forwarded.Values.AddRange(header.Value.OfType<string>());
            request.Headers.Add(forwarded);
        }

        try
        {
            var response = await connection.Client.HandleHttpAsync(request, deadline: DateTime.UtcNow.AddSeconds(30), cancellationToken: http.RequestAborted);
            if (response.Status is < 100 or > 599) return Results.StatusCode(StatusCodes.Status502BadGateway);

            http.Response.StatusCode = response.Status;
            foreach (var header in response.Headers.Where(header => !_notReturned.Contains(header.Name)))
            {
                http.Response.Headers[header.Name] = header.Values.ToArray();
            }

            await http.Response.Body.WriteAsync(response.Body.Memory, http.RequestAborted);
            return Results.Empty;
        }
        catch (RpcException exception)
        {
            if (exception.StatusCode == StatusCode.Unavailable) connections.Forget(system);
            return Results.StatusCode(exception.StatusCode == StatusCode.DeadlineExceeded ? StatusCodes.Status504GatewayTimeout : StatusCodes.Status502BadGateway);
        }
    }
}
