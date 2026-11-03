// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Semantics;
using Cratis.Stage.Contracts.Lifecycles;

namespace Cratis.Stage.Rendering.Cratis.Semantics.Lifecycles;

/// <summary>
/// Decides which state machines and workflows the renderer can emit, and names the rest with a <c language="text">STAGE-LIFECYCLE-*</c> diagnostic.
/// </summary>
/// <remarks>
/// What is refused fails the plan: a lifecycle is never silently left out of a rendered application.
/// </remarks>
internal static class SemanticLifecycleAdmission
{
    /// <summary>The diagnostic code for a construct the renderer does not emit yet.</summary>
    internal const string NotRendered = "STAGE-LIFECYCLE-010";

    internal static ImmutableArray<LifecycleRefusal> Check(SemanticApplicationContext context)
    {
        var refusals = ImmutableArray.CreateBuilder<LifecycleRefusal>();
        var application = context.Application;
        foreach (var machine in application.StateMachines)
        {
            var model = context.ReadModels[machine.ReadModel];
            foreach (var guard in machine.Transitions.Where(transition => transition.Guard is not null))
            {
                if (!SemanticLifecycleExpressions.CanRender(guard.Guard!, model))
                {
                    refusals.Add(new(LifecycleCodes.UnrenderedGuard, machine.Name, $"The guard of the transition on {context.Events[guard.Event].Name} in state machine '{machine.Name}' compares something other than a plain text, number or flag member of '{model.Name}'; the renderer emits only those."));
                }
            }

            foreach (var entry in machine.States.SelectMany(state => state.Entering).Where(entry => entry.Kind == SemanticStateEntryKind.SendCommand))
            {
                if (SemanticLifecycleExpressions.UnmappedProperty(context, context.Commands[entry.Command], model) is { } property)
                {
                    refusals.Add(new(NotRendered, machine.Name, $"Command '{context.Commands[entry.Command].Name}' sent when '{machine.Name}' enters a state has the property '{property}', which '{model.Name}' does not carry with the same name and type."));
                }
            }
        }

        foreach (var workflow in application.Workflows)
        {
            refusals.AddRange(WorkflowGraphSpecs.For(context.Request.ExecutionPlan, workflow).Refusals);
            foreach (var node in workflow.Nodes)
            {
                switch (node.Kind)
                {
                    case SemanticWorkflowNodeKind.Decision:
                        refusals.Add(new(NotRendered, workflow.Name, $"Decision '{node.Name}' of workflow '{workflow.Name}' reads the subject read model, which a rendered performer cannot do yet."));
                        break;
                    case SemanticWorkflowNodeKind.ForEach or SemanticWorkflowNodeKind.Join:
                        refusals.Add(new(NotRendered, workflow.Name, $"Node '{node.Name}' of workflow '{workflow.Name}' fans out over the subject, whose items a rendered trigger does not carry yet."));
                        break;
                    case SemanticWorkflowNodeKind.Approval:
                        refusals.Add(new(NotRendered, workflow.Name, $"Approval '{node.Name}' of workflow '{workflow.Name}' is decided through the Stage runtime's approvals endpoint; a rendered application exposes none."));
                        break;
                    case SemanticWorkflowNodeKind.Trigger when node.TriggerKind == SemanticWorkflowTriggerKind.Schedule:
                        refusals.Add(new(LifecycleCodes.ScheduledTrigger, workflow.Name, $"Trigger '{node.Name}' of workflow '{workflow.Name}' is a schedule; the renderer emits event, state and manual triggers."));
                        break;
                    case SemanticWorkflowNodeKind.Action when context.Commands[node.Command].Properties.Any(property => !property.IsGenerated && !property.IsIdentifier):
                        refusals.Add(new(NotRendered, workflow.Name, $"Command '{context.Commands[node.Command].Name}' sent by '{node.Name}' has more than its identifier; a rendered action passes only the subject key."));
                        break;
                }
            }
        }

        return refusals.ToImmutable();
    }
}
