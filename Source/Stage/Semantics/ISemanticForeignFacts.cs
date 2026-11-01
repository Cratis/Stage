// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics.Execution;

namespace Cratis.Stage.Semantics;

/// <summary>
/// Takes a foreign event delivered from an inbox: it joins the world, the reactions and captures it sets off settle, and what they append reaches the event log.
/// </summary>
internal interface ISemanticForeignFacts
{
    /// <summary>
    /// Receives a foreign fact.
    /// </summary>
    /// <param name="fact">The foreign fact.</param>
    /// <param name="occurrence">The occurrence the settlement runs under.</param>
    /// <returns>A task that completes once the fact is settled and its consequences are persisted.</returns>
    Task Receive(SemanticFact fact, SemanticCommandOccurrence occurrence);
}
