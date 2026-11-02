// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Nodes;
using Cratis.Orleans.Workflows;
using Cratis.Orleans.Workflows.Contracts;
using Cratis.Orleans.Workflows.Graphs;
using Cratis.Orleans.Workflows.Runs;
using Cratis.Orleans.Workflows.Runs.Coordination;
using Cratis.Orleans.Workflows.Triggers;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Lifecycles;
using Cratis.Stage.Semantics;

namespace Cratis.Stage.Host.Lifecycles;

/// <summary>
/// Performs lifecycle effects on the Orleans silo: commands go through the semantic runtime, workflow runs are admitted to the
/// Cratis.Orleans workflow engine and resumed when the fact they wait for is committed.
/// </summary>
/// <param name="plan">The execution plan of the loaded model.</param>
/// <param name="runtime">The semantic runtime commands are sent through.</param>
/// <param name="workflows">The workflow engine runs are admitted to.</param>
internal sealed class StageLifecycleEffects(SemanticExecutionPlan plan, ISemanticRuntime runtime, IWorkflows workflows) : ILifecycleEffects
{
    /// <summary>The workflow scope every Stage run belongs to; it is the Chronicle namespace the sandbox uses.</summary>
    internal static readonly WorkflowScope Scope = new("Default");

    readonly ConcurrentDictionary<(string Workflow, string Key), ConcurrentBag<WorkflowRunId>> _runs = new();

    /// <inheritdoc/>
    public async Task Send(SemanticCommand command, SemanticValue key, ImmutableArray<SemanticPropertyValue> subject)
    {
        var text = LifecycleSubjects.KeyText(key);
        var payload = LifecycleSubjects.CommandPayload(plan, command, text);
        var occurrence = new SemanticCommandOccurrence(DateTimeOffset.UtcNow, "state-machine", "state-machine", "state-machine");
        var result = await runtime.Execute(command, payload, new ClaimsPrincipal(), occurrence, false);
        if (result is not SemanticAccepted)
        {
            throw new InvalidOperationException($"Entering a state sent {command.Name} for {text}, which was not accepted: {result}");
        }
    }

    /// <inheritdoc/>
    public async Task Start(SemanticWorkflow workflow, SemanticValue key, string deduplication, ImmutableArray<SemanticPropertyValue> subject)
    {
        var text = LifecycleSubjects.KeyText(key);
        var payload = LifecycleSubjects.SubjectJson(plan, workflow.Subject, key, subject);
        var trigger = new WorkflowTrigger(
            WorkflowTriggerKind.StageHook,
            new TriggerSourceKey("stage"),
            new WorkflowDeduplicationKey($"{text}:{deduplication}"),
            new WorkflowSubjectReference(new WorkflowSubjectKind(plan.ReadModels[workflow.Subject].Name), new WorkflowSubjectId(text)),
            WorkflowContractReference.Any,
            new WorkflowData(payload.ToJsonString()),
            [],
            new WorkflowCorrelationKey(text),
            WorkflowCausationKey.None,
            WorkflowActor.System,
            DateTimeOffset.UtcNow);
        var admission = await workflows.Start(Scope, new WorkflowId(workflow.Name), trigger);
        if (!admission.Admitted)
        {
            throw new InvalidOperationException($"Workflow {workflow.Name} was not admitted for {text}: {string.Join("; ", admission.Rejections.Select(rejection => rejection.Value))}");
        }

        _runs.GetOrAdd((workflow.Name, text), _ => []).Add(admission.Run);
    }

    /// <inheritdoc/>
    public Task Resume(SemanticWorkflow workflow, string node, SemanticValue key) =>
        Resolve(workflow.Name, LifecycleSubjects.KeyText(key), node, WellKnownOutlets.Resumed.Value, "{}", WorkflowActor.System);

    /// <summary>
    /// Records a person's decision on the approval a run is waiting at.
    /// </summary>
    /// <param name="workflow">The workflow name.</param>
    /// <param name="subject">The subject key.</param>
    /// <param name="node">The approval node.</param>
    /// <param name="approved">Whether the person approved.</param>
    /// <param name="actor">Who decided.</param>
    /// <returns>Whether a run was waiting at the approval.</returns>
    internal Task<bool> Decide(string workflow, string subject, string node, bool approved, string actor) =>
        Resolve(workflow, subject, node, approved ? WellKnownOutlets.Approved.Value : WellKnownOutlets.Rejected.Value, "{}", new WorkflowActor(actor));

    /// <summary>
    /// Describes the runs the sandbox started for a workflow.
    /// </summary>
    /// <param name="workflow">The workflow name.</param>
    /// <returns>The runs with their status and the nodes they are waiting at.</returns>
    internal async Task<IReadOnlyList<StageWorkflowRun>> Runs(string workflow)
    {
        var runs = new List<StageWorkflowRun>();
        foreach (var ((name, subject), ids) in _runs.Where(entry => entry.Key.Workflow == workflow))
        {
            foreach (var id in ids.Distinct())
            {
                var snapshot = await workflows.GetRun(Scope, id).GetSnapshot();
                runs.Add(new(name, subject, id.Value, snapshot.Status.ToString(), [.. snapshot.Executions
                    .Where(execution => execution.Status == NodeExecutionStatus.Waiting).Select(execution => execution.Execution.Node.Value)])
                {
                    Nodes = [.. snapshot.Executions.Select(execution => $"{execution.Execution.Node.Value}={execution.Status}")]
                });
            }
        }

        return runs;
    }

    async Task<bool> Resolve(string workflow, string subject, string node, string outlet, string payload, WorkflowActor actor)
    {
        var resolved = false;
        if (!_runs.TryGetValue((workflow, subject), out var ids)) return false;
        foreach (var id in ids.Distinct())
        {
            var run = workflows.GetRun(Scope, id);
            var snapshot = await run.GetSnapshot();
            foreach (var execution in snapshot.Executions.Where(execution => execution.Status == NodeExecutionStatus.Waiting && execution.Execution.Node.Value == node))
            {
                await run.ResolveWait(WaitHandling.WaitIdFor(id, execution.Execution), outlet, new WorkflowData(payload), actor);
                resolved = true;
            }
        }

        return resolved;
    }
}

/// <summary>
/// A run the sandbox started, as the approvals endpoint reports it.
/// </summary>
/// <param name="Workflow">The workflow name.</param>
/// <param name="Subject">The subject key.</param>
/// <param name="Run">The run id.</param>
/// <param name="Status">The run status.</param>
/// <param name="WaitingAt">The nodes the run is suspended at.</param>
internal sealed record StageWorkflowRun(string Workflow, string Subject, string Run, string Status, IReadOnlyList<string> WaitingAt)
{
    /// <summary>Gets every node execution of the run with its status, in the order the engine recorded them.</summary>
    public IReadOnlyList<string> Nodes { get; init; } = [];
}
