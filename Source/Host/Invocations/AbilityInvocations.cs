// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Nodes;
using Cratis.Choreography.Workflows;
using Cratis.Choreography.Workflows.Adapters;
using Cratis.Choreography.Workflows.Contracts;
using Cratis.Choreography.Workflows.Runs;
using Cratis.Choreography.Workflows.Runs.Coordination;
using Cratis.Choreography.Workflows.Triggers;
using Cratis.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Cratis.Stage.Host.Invocations;

/// <summary>
/// The <see cref="IAbilityInvocations"/> over the invocation ledger and the workflow engine.
/// </summary>
/// <param name="catalog">The <see cref="IAbilityCatalog"/>.</param>
/// <param name="ledger">The <see cref="IInvocationLedger"/>.</param>
/// <param name="workflows">The workflow engine.</param>
/// <param name="authorizer">The <see cref="IInvocationAnswerAuthorizer"/>.</param>
/// <param name="durability">The <see cref="InvocationDurability"/> of the ledger.</param>
/// <param name="loggers">The logger factory.</param>
[IgnoreConvention]
public sealed class AbilityInvocations(IAbilityCatalog catalog, IInvocationLedger ledger, IWorkflows workflows, IInvocationAnswerAuthorizer authorizer, InvocationDurability durability, ILoggerFactory loggers) : IAbilityInvocations
{
    /// <summary>
    /// Gets the scope the invocation runs live in.
    /// </summary>
    public static readonly WorkflowScope Scope = new("Default");

    readonly ILogger _logger = loggers.CreateLogger("Cratis.Stage.Invocations");

    /// <inheritdoc/>
    public async Task<InvocationAdmission> Invoke(AbilityInvocation request)
    {
        var descriptor = catalog.Find(request.System, request.Ability);
        if (descriptor is null)
        {
            var refusal = $"The system {request.System} declares no ability {request.Ability}.";
            InvocationLog.Refused(_logger, $"{request.System}.{request.Ability}", refusal);
            return new(false, null, refusal);
        }

        if (durability.Refusal(descriptor) is { } volatileLedger)
        {
            InvocationLog.Refused(_logger, $"{request.System}.{request.Ability}", volatileLedger);
            return new(false, null, volatileLedger);
        }

        if (!IsJson(request.Input))
        {
            return new(false, null, $"The input of {descriptor.Reference.Capability} is not JSON.");
        }

        // The outbox comes first: the invocation exists, with its cause, before any run does.
        var record = await ledger.Record(descriptor, request.Origin, request.AnswerPolicy);
        var payload = new JsonObject
        {
            ["origin"] = new JsonObject
            {
                ["kind"] = request.Origin.Kind.ToString(),
                ["address"] = request.Origin.Address,
                ["causation"] = request.Origin.Causation
            },
            ["input"] = JsonNode.Parse(request.Input),
            ["answerPolicy"] = request.AnswerPolicy
        };
        var trigger = new WorkflowTrigger(
            WorkflowTriggerKind.StageHook,
            new TriggerSourceKey("stage"),
            new WorkflowDeduplicationKey(record.Identity),
            new WorkflowSubjectReference(new WorkflowSubjectKind(AbilityWorkflowDefinitions.SubjectKind), new WorkflowSubjectId(record.Identity)),
            WorkflowContractReference.Any,
            new WorkflowData(payload.ToJsonString()),
            [],
            new WorkflowCorrelationKey(record.Identity),
            new WorkflowCausationKey(request.Origin.Causation),
            WorkflowActor.System,
            DateTimeOffset.UtcNow);

        // A run that already exists for the identity is the same invocation, found again rather than started twice.
        var admission = await workflows.Start(Scope, new WorkflowId(descriptor.Reference.Workflow), trigger);
        if (!admission.Admitted)
        {
            var refusal = $"The engine did not admit the run of {descriptor.Reference.Capability}: {string.Join("; ", admission.Rejections.Select(rejection => rejection.Value))}";
            InvocationLog.Refused(_logger, record.Identity, refusal);
            return new(false, record, refusal);
        }

        var run = new WorkflowRunKey(Scope, admission.Run).ToString();
        await ledger.Of(record.Identity).Started(run);
        InvocationLog.Started(_logger, record.Identity, descriptor.Reference.Capability, run);
        return new(true, await ledger.Get(record.Identity), string.Empty);
    }

    /// <inheritdoc/>
    public async Task<InvocationAnswer> Answer(string identity, string outlet, string payload, ClaimsPrincipal answeredBy)
    {
        if (await ledger.Get(identity) is not { } record) return InvocationAnswer.Unknown;
        if (record.Status != InvocationStatus.Suspended || record.Run.Length == 0 || record.Execution.Length == 0) return InvocationAnswer.NotWaiting;
        if (!await authorizer.Authorize(record, answeredBy)) return InvocationAnswer.Forbidden;
        if (Describe(record) is not { } descriptor || !Answers(descriptor).Contains(outlet)) return InvocationAnswer.UnknownOutlet;

        var actor = Actor(answeredBy);
        var key = WorkflowRunKey.Parse(record.Run);
        var execution = NodeExecutionKey.Parse(record.Execution);

        // Who answered is recorded before the run takes the answer, so it survives whichever node ends the invocation. Taking an answer twice is harmless.
        await ledger.Of(identity).Decided(outlet, payload, actor);
        await workflows.GetRun(key.Scope, key.Run).ResolveWait(WaitHandling.WaitIdFor(key.Run, execution), outlet, new WorkflowData(payload), new WorkflowActor(actor));
        await ledger.Of(identity).Settle(outlet, payload, actor);
        InvocationLog.Answered(_logger, identity, actor, outlet);
        return InvocationAnswer.Answered;
    }

    /// <inheritdoc/>
    public async Task<InvocationReconciliation> Reconcile(string identity, string outcome, string output, string actor)
    {
        if (await ledger.Get(identity) is not { } record) return InvocationReconciliation.Unknown;
        if (!IsStopped(record)) return InvocationReconciliation.NotStopped;
        if (Describe(record) is not { } descriptor || !descriptor.Outlets.Contains(outcome)) return InvocationReconciliation.UnknownOutcome;

        var key = WorkflowRunKey.Parse(record.Run);
        var execution = NodeExecutionKey.Parse(record.Execution);
        var run = workflows.GetRun(key.Scope, key.Run);
        if (!await ReadyToReconcile(run, execution, record))
        {
            return InvocationReconciliation.NotStopped;
        }

        var chosen = outcome == AbilityDescriptor.FailedOutlet
            ? WorkflowNodeOutcome.Failed($"An operator recorded that {record.Capability} failed.")
            : WorkflowNodeOutcome.Succeeded(outcome, new WorkflowData(output.Length == 0 ? "{}" : output));

        // The choice is recorded before the run continues from it, so the settlement the run reaches carries who chose.
        await ledger.Of(identity).Decided(outcome, output, actor);
        await run.Reconcile(execution, chosen, new WorkflowActor(actor));
        await ledger.Of(identity).Settle(outcome, output, actor);
        InvocationLog.Reconciled(_logger, identity, actor, outcome);
        return InvocationReconciliation.Reconciled;
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<InvocationRecord>> List(InvocationStatus? status = null) => ledger.List(status);

    static bool IsStopped(InvocationRecord record) =>
        record.Status == InvocationStatus.Stopped && record.Run.Length > 0 && record.Execution.Length > 0;

    static async Task<bool> ReadyToReconcile(IWorkflowRun run, NodeExecutionKey execution, InvocationRecord record)
    {
        var snapshot = await run.GetSnapshot();
        var pending = snapshot.Executions.FirstOrDefault(candidate => candidate.Execution == execution);
        if (pending?.Status == NodeExecutionStatus.Scheduled && pending.Attempt.Value == record.Attempts)
        {
            // Stopped is persisted before the performer returns Ambiguous. Report that known outcome now;
            // the engine ignores the performer's duplicate report when it eventually arrives.
            await run.CompleteNode(execution, pending.Attempt, WorkflowNodeOutcome.Ambiguous(record.Reason));
            snapshot = await run.GetSnapshot();
        }

        return snapshot.Executions.Any(candidate => candidate.Execution == execution && candidate.Status == NodeExecutionStatus.ReconciliationRequired);
    }

    static bool IsJson(string text)
    {
        try
        {
            return JsonNode.Parse(text) is not null;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    static string Actor(ClaimsPrincipal principal) =>
        principal.Identity?.Name ?? principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? principal.FindFirstValue("sub") ?? "unknown";

    static IReadOnlyList<string> Answers(AbilityDescriptor descriptor) => [.. descriptor.Outlets.Where(outlet => outlet is not AbilityDescriptor.FailedOutlet and not AbilityDescriptor.ExpiredOutlet)];

    AbilityDescriptor? Describe(InvocationRecord record) =>
        catalog.All.FirstOrDefault(descriptor => descriptor.Reference.Capability == record.Capability);
}
