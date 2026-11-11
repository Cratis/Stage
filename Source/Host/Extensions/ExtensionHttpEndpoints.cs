// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Frozen;
using Cratis.Stage.Extensions.Protocol;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using RequestContext = Cratis.Stage.Extensions.Protocol.RequestContext;

namespace Cratis.Stage.Host.Extensions;

/// <summary>
/// Routes inbound HTTP requests to the backend that handles them, for a webhook or a callback an extension owns. The endpoint is anonymous by design, as a
/// webhook's caller is not a Stage user: the extension verifies the request, with the signature header its provider sends.
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

    static async Task<IResult> Handle(HttpContext http, string system, string? path, ExtensionConnections connections)
    {
        var relative = "/" + (path ?? string.Empty);
        if ((await connections.Connect(system, http.RequestAborted)).Connection is not { } connection) return Results.NotFound();
        if (!connection.Description.HttpRoutes.Any(route => Matches(route, relative))) return Results.NotFound();
        if (http.Request.ContentLength > MaxBodyBytes) return Results.StatusCode(StatusCodes.Status413PayloadTooLarge);

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

    static bool Matches(string route, string path)
    {
        var prefix = route.StartsWith('/') ? route : "/" + route;
        return path.Equals(prefix, StringComparison.Ordinal) || path.StartsWith(prefix.TrimEnd('/') + "/", StringComparison.Ordinal);
    }
}
