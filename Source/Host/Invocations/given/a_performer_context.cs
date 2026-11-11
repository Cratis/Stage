// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Choreography.Workflows;
using Cratis.Choreography.Workflows.Adapters;
using Cratis.Choreography.Workflows.Contracts;
using Cratis.Choreography.Workflows.Graphs;
using Cratis.Choreography.Workflows.Runs;
using Cratis.Stage.Host.Invocations.given;

namespace Cratis.Stage.Host.Invocations.given;

/// <summary>
/// The silo of the invocation specs with a performer to call directly, the way the engine calls it when a wave job is recovered.
/// </summary>
public abstract class a_performer_context : an_invocation_silo
{
    protected IWorkflowNodePerformer _performer = null!;
    protected AbilityDescriptor _descriptor = null!;

    protected abstract EffectClass Effect { get; }

    protected override IEnumerable<AbilityDescriptor> Abilities => [Describe("Pay", Effect)];

    protected WorkflowNodeInvocation Invocation(int attempt, string causation = "event 61") =>
        new(
            new(new("Default"), new("run-1")),
            NodeExecutionKey.RootOf(new("invoke")),
            new(attempt),
            new("idempotency-1"),
            new("{}"),
            new(new JsonObject
            {
                ["trigger"] = new JsonObject
                {
                    ["payload"] = new JsonObject
                    {
                        ["origin"] = new JsonObject { ["kind"] = nameof(InvocationOriginKind.ReactionBranch), ["address"] = "Fulfillment/Pay", ["causation"] = causation },
                        ["input"] = new JsonObject { ["amount"] = 5 }
                    }
                }
            }.ToJsonString()),
            []);

    void Establish()
    {
        _descriptor = Abilities.Single();
        _performer = new AbilityPerformer(_descriptor, Ledger, _backend, _app.Services.GetRequiredService<SystemGates>(), _app.Services.GetRequiredService<InvocationDurability>(), _app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("spec"));
    }
}
