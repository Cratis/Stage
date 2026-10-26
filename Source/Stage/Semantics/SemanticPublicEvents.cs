// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;

namespace Cratis.Stage.Semantics;

/// <summary>
/// Tells which of a model's events leave the application through its outbox, and which arrive from another application.
/// </summary>
internal static class SemanticPublicEvents
{
    /// <summary>
    /// Gets whether the event is a public contract this application publishes: Chronicle appends it to the outbox, never to the event log.
    /// </summary>
    /// <param name="contract">The event contract.</param>
    /// <returns><see langword="true"/> for a local public event.</returns>
    internal static bool IsPublished(SemanticEventContract contract) => contract.Visibility == SemanticEventVisibility.Public && contract.Origin is null;

    /// <summary>
    /// Gets whether the event is declared by another application and reaches this one through its inbox.
    /// </summary>
    /// <param name="contract">The event contract.</param>
    /// <returns><see langword="true"/> for a foreign event.</returns>
    internal static bool IsForeign(SemanticEventContract contract) => contract.Origin is not null;

    /// <summary>
    /// Counts the facts that land in the event log: a published event goes to the outbox and a foreign one is already in an inbox.
    /// </summary>
    /// <param name="plan">The plan declaring the events.</param>
    /// <param name="facts">The settled facts.</param>
    /// <returns>The number appended to the event log.</returns>
    internal static int Logged(SemanticExecutionPlan plan, IEnumerable<SemanticFact> facts) =>
        facts.Count(fact => !IsPublished(plan.Events[fact.EventContract]) && !IsForeign(plan.Events[fact.EventContract]));
}
