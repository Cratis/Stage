// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Nodes;
using Cratis.DependencyInjection;
using Cratis.Orleans.Workflows.Adapters;
using Cratis.Orleans.Workflows.Graphs;
using Cratis.Orleans.Workflows.Runs;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Contracts.Lifecycles;
using Cratis.Stage.Lifecycles;
using Cratis.Stage.Semantics;

namespace Cratis.Stage.Host.Lifecycles;

/// <summary>
/// Finds the performer of an action or decision node of a modeled workflow: actions send their command through the semantic
/// runtime, decisions evaluate their condition over the current subject read model.
/// </summary>
/// <param name="plan">The execution plan of the loaded model.</param>
/// <param name="runtime">Gets the semantic runtime commands are sent through.</param>
[IgnoreConvention]
internal sealed class StageWorkflowPerformers(SemanticExecutionPlan plan, Func<ISemanticRuntime> runtime) : IWorkflowNodePerformers
{
    readonly Dictionary<string, IWorkflowNodePerformer> _performers = Build(plan, runtime);

    /// <inheritdoc/>
    public IWorkflowNodePerformer? For(CapabilityReference capability) => _performers.GetValueOrDefault(capability.Id.Value);

    static Dictionary<string, IWorkflowNodePerformer> Build(SemanticExecutionPlan plan, Func<ISemanticRuntime> runtime)
    {
        var performers = new Dictionary<string, IWorkflowNodePerformer>(StringComparer.Ordinal);
        foreach (var command in plan.Commands.Values)
        {
            performers[WorkflowGraphSpecs.SendCapability(command.Name)] = new SendPerformer(plan, command, runtime);
        }

        foreach (var workflow in plan.Model.Application.Workflows)
        {
            foreach (var node in workflow.Nodes.Where(node => node.Kind == SemanticWorkflowNodeKind.Decision))
            {
                performers[WorkflowGraphSpecs.DecideCapability(workflow.Name, node.Name)] = new DecidePerformer(plan, workflow, node, runtime);
            }
        }

        return performers;
    }

    sealed class SendPerformer(SemanticExecutionPlan plan, SemanticCommand command, Func<ISemanticRuntime> runtime) : IWorkflowNodePerformer
    {
        public CapabilityReference Capability { get; } = new(WorkflowGraphSpecs.SendCapability(command.Name), 1);

        public async Task<WorkflowNodeOutcome> Perform(WorkflowNodeInvocation invocation, CancellationToken cancellationToken)
        {
            var input = JsonNode.Parse(invocation.Input.Value) as JsonObject ?? [];
            var key = input["trigger"]?["subject"]?["id"]?.GetValue<string>() ?? string.Empty;
            var payload = LifecycleSubjects.CommandPayload(plan, command, key, input["item"], input["trigger"]?["payload"]);
            var occurrence = new SemanticCommandOccurrence(DateTimeOffset.UtcNow, "workflow", "workflow", "workflow");
            var result = await runtime().Execute(command, payload, new ClaimsPrincipal(), occurrence, false);
            return result switch
            {
                SemanticAccepted => WorkflowNodeOutcome.Succeeded(WellKnownOutlets.Next),
                SemanticRejected rejected => WorkflowNodeOutcome.Failed($"{command.Name} was rejected: {rejected.Details}"),
                SemanticUnsupported unsupported => WorkflowNodeOutcome.Failed($"{command.Name} is not supported: {unsupported.Details}"),
                _ => WorkflowNodeOutcome.Failed($"{command.Name} did not settle.")
            };
        }
    }

    sealed class DecidePerformer(SemanticExecutionPlan plan, SemanticWorkflow workflow, SemanticWorkflowNode node, Func<ISemanticRuntime> runtime) : IWorkflowNodePerformer
    {
        public CapabilityReference Capability { get; } = new(WorkflowGraphSpecs.DecideCapability(workflow.Name, node.Name), 1);

        public async Task<WorkflowNodeOutcome> Perform(WorkflowNodeInvocation invocation, CancellationToken cancellationToken)
        {
            var input = JsonNode.Parse(invocation.Input.Value) as JsonObject ?? [];
            var key = input["trigger"]?["subject"]?["id"]?.GetValue<string>() ?? string.Empty;
            var instances = await runtime().ReadModels(workflow.Subject);
            var subject = instances.FirstOrDefault(instance => LifecycleSubjects.KeyText(instance.Key) == key);
            if (subject is null) return WorkflowNodeOutcome.Failed($"The subject '{key}' of {workflow.Name} is not in the read model {plan.ReadModels[workflow.Subject].Name}.");
            var values = subject.Values.ToDictionary(value => value.TargetProperty, value => value.Value);
            var holds = SemanticConditionEvaluator.Evaluate(node.Condition!, values);
            return WorkflowNodeOutcome.Succeeded(holds ? node.WhenTrue! : node.WhenFalse!);
        }
    }
}
