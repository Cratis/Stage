// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Stage.Contracts;

namespace Cratis.Stage.Host.Invocations;

/// <summary>
/// The endpoints the Workbench uses to show invocations and to reconcile the ones that stopped. They are tooling: a published runtime does not map them.
/// </summary>
public static class InvocationEndpoints
{
    /// <summary>
    /// Gets the route of the endpoints.
    /// </summary>
    public const string Route = "/stage/invocations";

    /// <summary>
    /// Maps the endpoints.
    /// </summary>
    /// <param name="app">The application.</param>
    public static void Map(WebApplication app)
    {
        app.MapGet(Route, async (string? status, IAbilityInvocations invocations) =>
        {
            InvocationStatus? parsed = Enum.TryParse<InvocationStatus>(status, true, out var value) ? value : null;
            if (status is not null && parsed is null) return Results.BadRequest($"'{status}' is not an invocation status.");
            var records = await invocations.List(parsed);
            return Results.Json(new { Invocations = records.Select(Shape), Stopped = records.Count(record => record.Status == InvocationStatus.Stopped) }, StageJson.Options);
        });

        app.MapPost($"{Route}/{{identity}}/reconciliation", async (string identity, Reconciliation reconciliation, IAbilityInvocations invocations) =>
            await invocations.Reconcile(identity, reconciliation.Outcome, reconciliation.Output ?? string.Empty, reconciliation.Actor ?? "operator") switch
            {
                InvocationReconciliation.Reconciled => Results.Accepted(),
                InvocationReconciliation.Unknown => Results.NotFound(),
                InvocationReconciliation.UnknownOutcome => Results.BadRequest($"'{reconciliation.Outcome}' is not an outcome of the ability."),
                _ => Results.Conflict("The invocation is not stopped for reconciliation.")
            });

        app.MapPost($"{Route}/{{identity}}/answer", async (string identity, Answer answer, HttpContext context, IAbilityInvocations invocations) =>
            await invocations.Answer(identity, answer.Outlet, answer.Payload ?? "{}", context.User) switch
            {
                InvocationAnswer.Answered => Results.Accepted(),
                InvocationAnswer.Unknown => Results.NotFound(),
                InvocationAnswer.Forbidden => Results.Forbid(),
                InvocationAnswer.UnknownOutlet => Results.BadRequest($"'{answer.Outlet}' is not an outlet of the ability."),
                _ => Results.Conflict("The invocation is not waiting for an answer.")
            });
    }

    static object Shape(InvocationRecord record) => new
    {
        record.Identity,
        record.Capability,
        Effect = record.Effect.ToString(),
        Status = record.Status.ToString(),
        Origin = new { Kind = record.Origin.Kind.ToString(), record.Origin.Address, record.Origin.Causation },
        record.Attempts,
        record.Outcome,
        record.Reason,
        record.DecidedBy,
        record.Run,
        record.PackageDigest,
        record.Occurred,
        record.Settled,
        record.AnswerPolicy
    };

    /// <summary>
    /// The outcome an operator found out happened.
    /// </summary>
    /// <param name="Outcome">The outcome: <c lang="text">returned</c>, a declared outcome, or <c lang="text">failed</c>.</param>
    /// <param name="Output">What the ability returned, as JSON.</param>
    /// <param name="Actor">The operator.</param>
    internal sealed record Reconciliation(string Outcome, string? Output, string? Actor);

    /// <summary>
    /// An answer to an invocation that waits.
    /// </summary>
    /// <param name="Outlet">The outlet the answer takes.</param>
    /// <param name="Payload">What the answer carries, as JSON.</param>
    internal sealed record Answer(string Outlet, string? Payload);
}
