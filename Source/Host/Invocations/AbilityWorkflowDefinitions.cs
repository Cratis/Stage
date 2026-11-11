// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Workflows;
using Cratis.Choreography.Workflows.Contracts;
using Cratis.Choreography.Workflows.Definitions;
using Cratis.Choreography.Workflows.Graphs;
using Cratis.DependencyInjection;

namespace Cratis.Stage.Host.Invocations;

/// <summary>
/// The workflow definitions of ability invocations: one definition per ability, whose run is one invocation. The run pins the extension package digest
/// the ability had when the invocation started, so upgrading an extension never changes an invocation in flight.
/// </summary>
/// <param name="catalog">The <see cref="IAbilityCatalog"/>.</param>
/// <param name="gates">The <see cref="SystemGates"/> that narrow the attempts.</param>
[IgnoreConvention]
internal sealed class AbilityWorkflowDefinitions(IAbilityCatalog catalog, SystemGates gates) : IWorkflowDefinitions
{
    /// <summary>
    /// Gets the area the definitions belong to.
    /// </summary>
    public const string Area = "stage-invocations";

    /// <summary>
    /// Gets the kind of subject an invocation run is about; its id is the invocation identity.
    /// </summary>
    public const string SubjectKind = "AbilityInvocation";

    /// <summary>
    /// Gets the name of the node that performs the ability.
    /// </summary>
    public const string InvokeNode = "invoke";

    const int SettleAttempts = 5;

    readonly Dictionary<string, WorkflowDefinitionSnapshot> _snapshots =
        catalog.All.ToDictionary(descriptor => descriptor.Reference.Workflow, descriptor => Snapshot(descriptor, gates), StringComparer.Ordinal);

    /// <inheritdoc/>
    public bool Owns(WorkflowKey workflow) => _snapshots.ContainsKey(workflow.Workflow.Value);

    /// <inheritdoc/>
    public Task<WorkflowDefinitionSnapshot?> GetActive(WorkflowKey workflow) =>
        Task.FromResult(_snapshots.GetValueOrDefault(workflow.Workflow.Value));

    static WorkflowDefinitionSnapshot Snapshot(AbilityDescriptor descriptor, SystemGates gates)
    {
        var outlets = descriptor.Outlets;
        var nodes = new List<WorkflowNode>
        {
            new("trigger", WorkflowNodeKind.Trigger, new CapabilityReference("workflows.trigger.stagehook", 1), "{}", [new OutletName("next")], new WorkflowNodePolicy(1, TimeSpan.FromMinutes(1), TimeSpan.FromSeconds(1), 1)),
            new(InvokeNode, WorkflowNodeKind.Action, new CapabilityReference(descriptor.Reference.CapabilityId, descriptor.Reference.ContractMajor), "{}", outlets.Select(outlet => new OutletName(outlet)), new WorkflowNodePolicy(Math.Max(1, gates.Attempts(descriptor)), descriptor.Timeout, descriptor.RetryDelay, 1))
        };
        var edges = new List<WorkflowEdge> { new("trigger", "next", InvokeNode) };
        foreach (var outlet in outlets)
        {
            var settle = $"settle-{outlet}";
            nodes.Add(new(settle, WorkflowNodeKind.Action, new CapabilityReference(SettlePerformer.CapabilityId, 1), $$"""{"outcome":"{{outlet}}"}""", [new OutletName("next"), new OutletName("failed")], new WorkflowNodePolicy(SettleAttempts, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(5), 1)));
            edges.Add(new(InvokeNode, outlet, settle));
        }

        return new(
            new WorkflowId(descriptor.Reference.Workflow),
            new WorkflowAreaKey(Area),
            new WorkflowVersion(1),
            new WorkflowSubjectKind(SubjectKind),
            new WorkflowGraph([.. nodes], [.. edges]),
            new WorkflowCatalogVersion(descriptor.PackageDigest),
            [new CapabilityPin(new CapabilityReference(descriptor.Reference.CapabilityId, descriptor.Reference.ContractMajor), new ProviderVersion(descriptor.PackageDigest))]);
    }
}
