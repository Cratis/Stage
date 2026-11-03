// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;

namespace Cratis.Stage.Contracts.Lifecycles;

/// <summary>
/// Decides which state machines and workflows of a model the Stage runtime drives.
/// </summary>
/// <remarks>
/// Whatever is refused is named with a <c>STAGE-LIFECYCLE-*</c> diagnostic and left out; it is never skipped silently. Its
/// specifications are still verified by the reference evaluator, which does not depend on what the runtime can drive.
/// </remarks>
public static class LifecycleAdmission
{
    /// <summary>
    /// Finds what the runtime cannot drive.
    /// </summary>
    /// <param name="plan">The execution plan.</param>
    /// <returns>The refusals, in model order.</returns>
    public static ImmutableArray<LifecycleRefusal> ForRuntime(SemanticExecutionPlan plan)
    {
        var refusals = ImmutableArray.CreateBuilder<LifecycleRefusal>();
        var refusedWorkflows = new HashSet<string>(StringComparer.Ordinal);
        foreach (var graph in WorkflowGraphSpecs.For(plan))
        {
            var found = graph.Refusals.ToList();
            foreach (var trigger in graph.Workflow.Nodes.Where(node => node.Kind == SemanticWorkflowNodeKind.Trigger && node.TriggerKind == SemanticWorkflowTriggerKind.Schedule))
            {
                found.Add(new(LifecycleCodes.ScheduledTrigger, graph.Workflow.Name, $"Trigger '{trigger.Name}' of workflow '{graph.Workflow.Name}' is a schedule; the runtime starts runs from events, states and by hand only."));
            }

            if (found.Count > 0) refusedWorkflows.Add(graph.Workflow.Name);
            refusals.AddRange(found);
        }

        foreach (var machine in plan.Model.Application.StateMachines)
        {
            foreach (var started in machine.States.SelectMany(state => state.Entering).Where(entry => entry.Kind == SemanticStateEntryKind.StartWorkflow && refusedWorkflows.Contains(entry.Workflow!)))
            {
                refusals.Add(new(LifecycleCodes.SingleTrigger, machine.Name, $"State machine '{machine.Name}' starts workflow '{started.Workflow}', which the runtime does not run; the entering action is left out."));
            }
        }

        return refusals.ToImmutable();
    }

    /// <summary>
    /// Gets the names of the workflows the runtime drives.
    /// </summary>
    /// <param name="plan">The execution plan.</param>
    /// <returns>The executable workflow names.</returns>
    public static IReadOnlySet<string> ExecutableWorkflows(SemanticExecutionPlan plan)
    {
        var refused = ForRuntime(plan).Select(refusal => refusal.Owner).ToHashSet(StringComparer.Ordinal);
        return plan.Model.Application.Workflows.Select(workflow => workflow.Name).Where(name => !refused.Contains(name)).ToHashSet(StringComparer.Ordinal);
    }
}
