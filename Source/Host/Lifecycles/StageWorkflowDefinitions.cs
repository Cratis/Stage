// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Workflows;
using Cratis.Choreography.Workflows.Definitions;
using Cratis.Choreography.Workflows.Graphs;
using Cratis.DependencyInjection;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Contracts.Lifecycles;

namespace Cratis.Stage.Host.Lifecycles;

/// <summary>
/// Serves the workflows of the loaded model to the Cratis.Choreography workflow engine as definition snapshots.
/// </summary>
[IgnoreConvention]
internal sealed class StageWorkflowDefinitions : IWorkflowDefinitions
{
    /// <summary>The area every Stage workflow belongs to.</summary>
    internal const string Area = "stage";

    readonly Dictionary<string, WorkflowDefinitionSnapshot> _snapshots;

    StageWorkflowDefinitions(Dictionary<string, WorkflowDefinitionSnapshot> snapshots) => _snapshots = snapshots;

    /// <inheritdoc/>
    public bool Owns(WorkflowKey workflow) => _snapshots.ContainsKey(workflow.Workflow.Value);

    /// <inheritdoc/>
    public Task<WorkflowDefinitionSnapshot?> GetActive(WorkflowKey workflow) =>
        Task.FromResult(_snapshots.GetValueOrDefault(workflow.Workflow.Value));

    /// <summary>
    /// Builds the snapshots of every workflow the runtime drives; a refused workflow has none, so a start for it is rejected.
    /// </summary>
    /// <param name="plan">The execution plan.</param>
    /// <returns>The definitions.</returns>
    internal static StageWorkflowDefinitions For(SemanticExecutionPlan plan)
    {
        var executable = LifecycleAdmission.ExecutableWorkflows(plan);
        var snapshots = WorkflowGraphSpecs.For(plan)
            .Where(graph => executable.Contains(graph.Workflow.Name))
            .ToDictionary(graph => graph.Workflow.Name, graph => Snapshot(plan, graph), StringComparer.Ordinal);
        return new(snapshots);
    }

    /// <summary>
    /// Translates one workflow graph to an engine definition snapshot.
    /// </summary>
    /// <param name="plan">The execution plan.</param>
    /// <param name="graph">The translated workflow.</param>
    /// <returns>The snapshot.</returns>
    internal static WorkflowDefinitionSnapshot Snapshot(SemanticExecutionPlan plan, WorkflowGraphSpec graph)
    {
        var nodes = graph.Nodes.Select(node => new WorkflowNode(
            node.Name,
            Kind(node.Kind),
            new CapabilityReference(node.Capability, 1),
            node.ConfigurationJson,
            node.Outlets.Select(outlet => new OutletName(outlet)),
            new WorkflowNodePolicy(node.MaxAttempts, node.AttemptTimeout, node.RetryDelay, 1)));
        var edges = graph.Edges.Select(edge => new WorkflowEdge(edge.From, edge.Outlet, edge.To));
        return new(
            new WorkflowId(graph.Workflow.Name),
            new WorkflowAreaKey(Area),
            new WorkflowVersion(1),
            new Cratis.Choreography.Workflows.Contracts.WorkflowSubjectKind(plan.ReadModels[graph.Workflow.Subject].Name),
            new WorkflowGraph([.. nodes], [.. edges]),
            new WorkflowCatalogVersion(plan.Revision.ToString()),
            []);
    }

    static WorkflowNodeKind Kind(SemanticWorkflowNodeKind kind) => kind switch
    {
        SemanticWorkflowNodeKind.Trigger => WorkflowNodeKind.Trigger,
        SemanticWorkflowNodeKind.Action => WorkflowNodeKind.Action,
        SemanticWorkflowNodeKind.Decision => WorkflowNodeKind.Decision,
        SemanticWorkflowNodeKind.Wait => WorkflowNodeKind.Wait,
        SemanticWorkflowNodeKind.Approval => WorkflowNodeKind.Approval,
        SemanticWorkflowNodeKind.ForEach => WorkflowNodeKind.ForEach,
        SemanticWorkflowNodeKind.Join => WorkflowNodeKind.Join,
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "The workflow node kind has no engine counterpart.")
    };
}
