// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Semantics;

namespace Cratis.Stage.Lifecycles;

/// <summary>
/// Performs what a state machine entering a state, or a fact reaching a workflow, asks for.
/// </summary>
public interface ILifecycleEffects
{
    /// <summary>
    /// Sends a command on behalf of a state a machine entered.
    /// </summary>
    /// <param name="command">The command.</param>
    /// <param name="key">The key of the instance, which is the identifier of the command.</param>
    /// <param name="subject">The current values of the followed read model instance; empty when there is none.</param>
    /// <returns>A task that completes when the command was sent.</returns>
    Task Send(SemanticCommand command, SemanticValue key, ImmutableArray<SemanticPropertyValue> subject);

    /// <summary>
    /// Invokes an ability on behalf of a state a machine entered.
    /// </summary>
    /// <param name="invocation">The state entry that invokes the ability.</param>
    /// <param name="subject">The current values of the followed read model instance; empty when there is none.</param>
    /// <returns>A task that completes when the invocation was admitted.</returns>
    Task Invoke(StateEntryInvocation invocation, ImmutableArray<SemanticPropertyValue> subject);

    /// <summary>
    /// Starts a run of a workflow for one subject.
    /// </summary>
    /// <param name="workflow">The workflow.</param>
    /// <param name="key">The key of the subject read model instance.</param>
    /// <param name="deduplication">Identifies what started the run, so a repeated start finds the run it started.</param>
    /// <param name="subject">The current values of the subject read model instance; empty when there is none.</param>
    /// <returns>A task that completes when the run was admitted.</returns>
    Task Start(SemanticWorkflow workflow, SemanticValue key, string deduplication, ImmutableArray<SemanticPropertyValue> subject);

    /// <summary>
    /// Resolves the waits for an event that open runs of a workflow hold for one subject.
    /// </summary>
    /// <param name="workflow">The workflow.</param>
    /// <param name="node">The wait node.</param>
    /// <param name="key">The key of the subject read model instance.</param>
    /// <returns>A task that completes when the waits were resolved.</returns>
    Task Resume(SemanticWorkflow workflow, string node, SemanticValue key);
}
