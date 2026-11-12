// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Stage.Contracts;

namespace Cratis.Stage.Host.Extensions.Inbound;

/// <summary>
/// The endpoints the Workbench uses to show the traffic that reached the extensions and to retry what could not be delivered. They are tooling: a published
/// runtime does not map them. They show that a request arrived and what became of it, never its body or the values of its headers, as those can carry secrets.
/// </summary>
public static class InboundDeliveryEndpoints
{
    /// <summary>
    /// Gets the route of the endpoints.
    /// </summary>
    public const string Route = "/stage/extensions/deliveries";

    /// <summary>
    /// Maps the endpoints.
    /// </summary>
    /// <param name="endpoints">The route builder.</param>
    public static void Map(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet(Route, async (string? status, IInboundDeliveries deliveries) =>
        {
            InboundStatus? parsed = Enum.TryParse<InboundStatus>(status, true, out var value) ? value : null;
            if (status is not null && parsed is null) return Results.BadRequest($"'{status}' is not an inbound delivery status.");
            var listed = await deliveries.List(parsed);
            return Results.Json(new { Deliveries = listed.Select(Shape), Failed = listed.Count(delivery => delivery.Status == InboundStatus.Failed) }, StageJson.Options);
        });

        endpoints.MapPost($"{Route}/retry", async (Retry retry, IInboundDeliveries deliveries, InboundDeliveryDispatcher dispatcher) =>
        {
            if (await deliveries.Get(retry.Id) is null) return Results.NotFound();
            if (!await deliveries.Requeue(retry.Id)) return Results.Conflict("Only a delivery that failed or was rejected can be retried.");
            dispatcher.Wake();
            return Results.Accepted();
        });
    }

    static object Shape(InboundDelivery delivery) => new
    {
        delivery.Id,
        delivery.System,
        delivery.Method,
        delivery.Path,
        delivery.Query,
        Status = delivery.Status.ToString(),
        delivery.Attempts,
        delivery.NextAttempt,
        delivery.ResponseStatus,
        delivery.Reason,
        delivery.Received,
        delivery.Completed,
        delivery.Replays,
        BodyBytes = delivery.Body.Length,
        Headers = delivery.Headers.Select(header => header.Name)
    };

    /// <summary>
    /// A decision to retry a delivery.
    /// </summary>
    /// <param name="Id">The identity of the delivery.</param>
    internal sealed record Retry(string Id);
}
