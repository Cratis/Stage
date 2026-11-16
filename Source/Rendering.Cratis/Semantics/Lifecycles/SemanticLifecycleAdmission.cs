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
        foreach (var machine in context.Application.StateMachines)
        {
            CheckMachine(context, machine, refusals);
        }

        foreach (var workflow in context.Application.Workflows)
        {
            refusals.AddRange(WorkflowGraphSpecs.For(context.Request.ExecutionPlan, workflow).Refusals);
            refusals.AddRange(workflow.Nodes.Select(node => CheckNode(context, workflow, node)).OfType<LifecycleRefusal>());
        }

        return refusals.ToImmutable();
    }

    static void CheckMachine(SemanticApplicationContext context, SemanticStateMachine machine, ImmutableArray<LifecycleRefusal>.Builder refusals)
    {
        var model = context.ReadModels[machine.ReadModel];
        foreach (var guard in machine.Transitions.Where(transition => transition.Guard is not null && !SemanticLifecycleExpressions.CanRender(transition.Guard, model)))
        {
            refusals.Add(new(LifecycleCodes.UnrenderedGuard, machine.Name, $"The guard of the transition on {context.Events[guard.Event].Name} in state machine '{machine.Name}' compares something other than a plain text, number or flag member of '{model.Name}'; the renderer emits only those."));
        }

        foreach (var entry in machine.States.SelectMany(state => state.Entering).Where(entry => entry.Kind == SemanticStateEntryKind.SendCommand))
        {
            if (SemanticLifecycleExpressions.UnmappedProperty(context, context.Commands[entry.Command], model) is { } property)
            {
                refusals.Add(new(NotRendered, machine.Name, $"Command '{context.Commands[entry.Command].Name}' sent when '{machine.Name}' enters a state has the property '{property}', which '{model.Name}' does not carry with the same name and type."));
            }
        }

        foreach (var (state, entry) in machine.States.SelectMany(state => state.Entering.Where(entry => entry.Kind == SemanticStateEntryKind.InvokeAbility).Select(entry => (state, entry))))
        {
            refusals.Add(new(NotRendered, machine.Name, $"Entering '{state.Name}' of state machine '{machine.Name}' invokes the ability '{entry.Ability}'; the Stage runtime invokes it, a rendered application does not yet."));
        }
    }

    static LifecycleRefusal? CheckNode(SemanticApplicationContext context, SemanticWorkflow workflow, SemanticWorkflowNode node) => node.Kind switch
    {
        SemanticWorkflowNodeKind.Decision =>
            new(NotRendered, workflow.Name, $"Decision '{node.Name}' of workflow '{workflow.Name}' reads the subject read model, which a rendered performer cannot do yet."),
        SemanticWorkflowNodeKind.ForEach or SemanticWorkflowNodeKind.Join =>
            new(NotRendered, workflow.Name, $"Node '{node.Name}' of workflow '{workflow.Name}' fans out over the subject, whose items a rendered trigger does not carry yet."),
        SemanticWorkflowNodeKind.Approval =>
            new(NotRendered, workflow.Name, $"Approval '{node.Name}' of workflow '{workflow.Name}' is decided through the Stage runtime's approvals endpoint; a rendered application exposes none."),
        SemanticWorkflowNodeKind.Trigger when node.TriggerKind == SemanticWorkflowTriggerKind.Schedule =>
            new(LifecycleCodes.ScheduledTrigger, workflow.Name, $"Trigger '{node.Name}' of workflow '{workflow.Name}' is a schedule; the renderer emits event, state and manual triggers."),
        SemanticWorkflowNodeKind.Action when !string.IsNullOrEmpty(node.Ability) =>
            new(NotRendered, workflow.Name, $"Action '{node.Name}' of workflow '{workflow.Name}' invokes the ability '{node.Ability}'; the Stage runtime invokes it, a rendered application does not yet."),
        SemanticWorkflowNodeKind.Action when context.Commands[node.Command].Properties.Any(property => !property.IsGenerated && !property.IsIdentifier) =>
            new(NotRendered, workflow.Name, $"Command '{context.Commands[node.Command].Name}' sent by '{node.Name}' has more than its identifier; a rendered action passes only the subject key."),
        _ => null
    };
}
