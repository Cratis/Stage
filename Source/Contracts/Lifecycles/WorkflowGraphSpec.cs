// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Semantics;

namespace Cratis.Stage.Contracts.Lifecycles;

/// <summary>
/// One node of a Screenplay workflow as the Cratis.Choreography workflow engine models it.
/// </summary>
/// <param name="Name">The node name, which is the engine node id.</param>
/// <param name="Kind">The node kind.</param>
/// <param name="Capability">The engine capability id the node references.</param>
/// <param name="ConfigurationJson">The engine node configuration, in the engine's JSON form.</param>
/// <param name="Outlets">The outlets the node declares, in the engine's names.</param>
/// <param name="MaxAttempts">The most attempts of one execution.</param>
/// <param name="AttemptTimeout">How long one attempt may take.</param>
/// <param name="RetryDelay">How long to wait before the next attempt.</param>
/// <param name="Source">The Screenplay node.</param>
public sealed record WorkflowGraphNodeSpec(
    string Name,
    SemanticWorkflowNodeKind Kind,
    string Capability,
    string ConfigurationJson,
    ImmutableArray<string> Outlets,
    int MaxAttempts,
    TimeSpan AttemptTimeout,
    TimeSpan RetryDelay,
    SemanticWorkflowNode Source);

/// <summary>
/// One edge from an outlet of a node to another node, in the engine's names.
/// </summary>
/// <param name="From">The node the edge leaves.</param>
/// <param name="Outlet">The engine outlet it leaves by.</param>
/// <param name="To">The node it enters.</param>
public sealed record WorkflowGraphEdgeSpec(string From, string Outlet, string To);

/// <summary>
/// A Screenplay workflow translated to the graph of the Cratis.Choreography workflow engine, or the reasons it cannot be.
/// </summary>
/// <param name="Workflow">The Screenplay workflow.</param>
/// <param name="Nodes">The engine nodes.</param>
/// <param name="Edges">The engine edges.</param>
/// <param name="Refusals">What the engine cannot express; when any, the graph must not be run or rendered.</param>
public sealed record WorkflowGraphSpec(
    SemanticWorkflow Workflow,
    ImmutableArray<WorkflowGraphNodeSpec> Nodes,
    ImmutableArray<WorkflowGraphEdgeSpec> Edges,
    ImmutableArray<LifecycleRefusal> Refusals)
{
    /// <summary>
    /// Gets whether the engine can run this workflow.
    /// </summary>
    public bool IsExecutable => Refusals.IsEmpty;
}
