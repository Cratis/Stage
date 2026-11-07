// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Globalization;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;

namespace Cratis.Stage.Contracts.Lifecycles;

/// <summary>
/// Translates Screenplay workflows to the graph model of the Cratis.Choreography workflow engine.
/// </summary>
/// <remarks>
/// The runtime builds its definition snapshots from this translation and the renderer emits the same graph as code, so what runs
/// in the sandbox and what is rendered cannot drift apart. The outlet names are the engine's <c>WellKnownOutlets</c>.
/// </remarks>
public static class WorkflowGraphSpecs
{
    const string DefaultFanOut = "10";

    /// <summary>
    /// Gets the capability id of the performer that sends a command.
    /// </summary>
    /// <param name="command">The command name.</param>
    /// <returns>The capability id.</returns>
    public static string SendCapability(string command) => $"stage.send.{command}";

    /// <summary>
    /// Gets the capability id of the performer that decides a decision node.
    /// </summary>
    /// <param name="workflow">The workflow name.</param>
    /// <param name="node">The node name.</param>
    /// <returns>The capability id.</returns>
    public static string DecideCapability(string workflow, string node) => $"stage.decide.{workflow}.{node}";

    /// <summary>
    /// Translates every workflow of a plan.
    /// </summary>
    /// <param name="plan">The execution plan.</param>
    /// <returns>One translation per workflow, in model order.</returns>
    public static ImmutableArray<WorkflowGraphSpec> For(SemanticExecutionPlan plan) =>
        [.. plan.Model.Application.Workflows.Select(workflow => For(plan, workflow))];

    /// <summary>
    /// Translates one workflow.
    /// </summary>
    /// <param name="plan">The execution plan.</param>
    /// <param name="workflow">The workflow.</param>
    /// <returns>The translation, with its refusals.</returns>
    public static WorkflowGraphSpec For(SemanticExecutionPlan plan, SemanticWorkflow workflow)
    {
        var refusals = new List<LifecycleRefusal>();
        var triggers = workflow.Nodes.Where(node => node.Kind == SemanticWorkflowNodeKind.Trigger).ToArray();
        if (triggers.Length != 1)
        {
            refusals.Add(new(LifecycleCodes.SingleTrigger, workflow.Name, $"Workflow '{workflow.Name}' declares {triggers.Length} triggers; the engine runs a graph with exactly one trigger node. Split it into one workflow per trigger."));
        }

        var (joins, forEachOf) = Pair(workflow, refusals);
        var nodes = workflow.Nodes.Select(node => Node(plan, workflow, node, triggers.FirstOrDefault()?.Name ?? string.Empty, joins, refusals)).ToImmutableArray();
        var edges = Edges(workflow, nodes, forEachOf, joins, refusals);
        return new(workflow, nodes, edges, [.. refusals]);
    }

    static WorkflowGraphNodeSpec Node(SemanticExecutionPlan plan, SemanticWorkflow workflow, SemanticWorkflowNode node, string trigger, Dictionary<string, string> joins, List<LifecycleRefusal> refusals)
    {
        var name = node.Name;
        WorkflowGraphNodeSpec Make(string capability, string configuration, params string[] outlets) => new(
            name,
            node.Kind,
            capability,
            configuration,
            [.. outlets],
            Math.Max(1, node.MaxAttempts ?? 1),
            node.AttemptTimeout is { } timeout ? TimeSpan.FromSeconds(timeout) : TimeSpan.FromMinutes(10),
            node.RetryDelay is { } delay ? TimeSpan.FromSeconds(delay) : TimeSpan.FromSeconds(30),
            node);

        switch (node.Kind)
        {
            case SemanticWorkflowNodeKind.Trigger:
                return Make($"workflows.trigger.{node.TriggerKind.ToString()!.ToLowerInvariant()}", "{}", "next");
            case SemanticWorkflowNodeKind.Action:
                return Make(SendCapability(plan.Commands[node.Command].Name), "{}", "next", "failed");
            case SemanticWorkflowNodeKind.Decision:
                return Make(DecideCapability(workflow.Name, name), "{}", node.WhenTrue ?? string.Empty, node.WhenFalse ?? string.Empty, "failed");
            case SemanticWorkflowNodeKind.Wait when node.Duration is { } duration:
                return Make("workflows.wait.timer", $$"""{"kind":"timer","after":"{{Span(duration)}}"}""", "elapsed");
            case SemanticWorkflowNodeKind.Wait:
                var signal = plan.Events[node.Event].Name;
                var expiry = node.ExpiresAfter is { } expires ? $"\"expiresAfter\":\"{Span(expires)}\"," : string.Empty;
                return Make("workflows.wait.signal", $$$"""{"kind":"signal",{{{expiry}}}"signal":{"id":"{{{signal}}}","version":1}}""", "resumed", "expired", "cancelled");
            case SemanticWorkflowNodeKind.Approval:
                var approvalExpiry = node.ExpiresAfter is { } approves ? $",\"expiresAfter\":\"{Span(approves)}\"" : string.Empty;
                return Make("workflows.approval", $$"""{"kind":"approval"{{approvalExpiry}}}""", "approved", "rejected", "expired");
            case SemanticWorkflowNodeKind.ForEach:
                var items = plan.ReadModels[workflow.Subject].Properties.Single(property => property.Id == node.Items).Name;
                var fanOut = node.MaxParallel is { } parallel ? parallel.ToString(CultureInfo.InvariantCulture) : DefaultFanOut;
                return Make("workflows.for-each", $$$"""{"itemsFrom":"{{{trigger}}}","itemsProperty":"{{{CamelCase(items)}}}","maxParallelism":{{{fanOut}}}}""", "each", "failed", "fallback");
            case SemanticWorkflowNodeKind.Join:
                var forEach = joins.GetValueOrDefault(name) ?? string.Empty;
                var (mode, threshold) = node.JoinMode switch
                {
                    SemanticJoinMode.Any => ("any", 0),
                    SemanticJoinMode.AtLeast => ("atLeast", node.JoinCount ?? 0),
                    _ => ("all", 0)
                };
                return Make("workflows.join", $$"""{"forEach":"{{forEach}}","mode":"{{mode}}","threshold":{{threshold}}}""", "completed", "partial");
            default:
                refusals.Add(new(LifecycleCodes.UnknownNode, workflow.Name, $"Node '{name}' has the unknown kind {node.Kind}."));
                return Make("workflows.unknown", "{}");
        }
    }

    // Each foreach is gathered by exactly one join: the first one its branches reach. The engine keys a join by its foreach.
    static (Dictionary<string, string> JoinToForEach, Dictionary<string, string> ForEachToJoin) Pair(SemanticWorkflow workflow, List<LifecycleRefusal> refusals)
    {
        var byName = workflow.Nodes.ToDictionary(node => node.Name, StringComparer.Ordinal);
        var joinToForEach = new Dictionary<string, string>(StringComparer.Ordinal);
        var forEachToJoin = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var forEach in workflow.Nodes.Where(node => node.Kind == SemanticWorkflowNodeKind.ForEach))
        {
            var reached = new HashSet<string>(StringComparer.Ordinal);
            var found = new List<string>();
            var pending = new Queue<string>(workflow.Edges.Where(edge => edge.From == forEach.Name && edge.Outlet == "each").Select(edge => edge.To));
            while (pending.Count > 0)
            {
                var next = pending.Dequeue();
                if (!reached.Add(next) || !byName.TryGetValue(next, out var node)) continue;
                if (node.Kind == SemanticWorkflowNodeKind.Join)
                {
                    found.Add(next);
                    continue;
                }

                if (node.Kind == SemanticWorkflowNodeKind.ForEach)
                {
                    refusals.Add(new(LifecycleCodes.NestedForEach, workflow.Name, $"Foreach '{node.Name}' is inside the branches of '{forEach.Name}'; the engine does not run nested fan-outs."));
                    continue;
                }

                foreach (var edge in workflow.Edges.Where(edge => edge.From == next)) pending.Enqueue(edge.To);
            }

            if (found.Count != 1)
            {
                refusals.Add(new(LifecycleCodes.ForEachJoin, workflow.Name, $"Foreach '{forEach.Name}' needs exactly one join after its branches; it reaches {found.Count}."));
                continue;
            }

            forEachToJoin[forEach.Name] = found[0];
            joinToForEach[found[0]] = forEach.Name;
        }

        foreach (var join in workflow.Nodes.Where(node => node.Kind == SemanticWorkflowNodeKind.Join && !joinToForEach.ContainsKey(node.Name)))
        {
            refusals.Add(new(LifecycleCodes.UnpairedJoin, workflow.Name, $"Join '{join.Name}' does not gather the branches of a foreach; the engine only joins the branches of a fan-out."));
        }

        return (joinToForEach, forEachToJoin);
    }

    static ImmutableArray<WorkflowGraphEdgeSpec> Edges(SemanticWorkflow workflow, ImmutableArray<WorkflowGraphNodeSpec> nodes, Dictionary<string, string> forEachToJoin, Dictionary<string, string> joinToForEach, List<LifecycleRefusal> refusals)
    {
        _ = joinToForEach;
        var byName = nodes.ToDictionary(node => node.Name, StringComparer.Ordinal);
        var edges = ImmutableArray.CreateBuilder<WorkflowGraphEdgeSpec>();
        foreach (var edge in workflow.Edges)
        {
            if (!byName.TryGetValue(edge.From, out var from)) continue;
            var outlet = edge.Outlet;
            var origin = edge.From;
            switch (from.Kind)
            {
                case SemanticWorkflowNodeKind.Trigger:
                    outlet = "next";
                    break;
                case SemanticWorkflowNodeKind.Wait when from.Source.Duration is not null && outlet == "resumed":
                    outlet = "elapsed";
                    break;
                case SemanticWorkflowNodeKind.ForEach when outlet == "completed":
                    if (!forEachToJoin.TryGetValue(edge.From, out var join)) continue;
                    origin = join;
                    break;
                case SemanticWorkflowNodeKind.Join when outlet == "next":
                    outlet = "completed";
                    break;
            }

            if (!byName[origin].Outlets.Contains(outlet))
            {
                refusals.Add(new(LifecycleCodes.UnknownOutlet, workflow.Name, $"Node '{origin}' has no outlet '{outlet}' in the engine."));
                continue;
            }

            edges.Add(new(origin, outlet, edge.To));
        }

        return edges.ToImmutable();
    }

    static string Span(long seconds) => TimeSpan.FromSeconds(seconds).ToString("c", CultureInfo.InvariantCulture);

    static string CamelCase(string name) => name.Length == 0 ? name : char.ToLowerInvariant(name[0]) + name[1..];
}
