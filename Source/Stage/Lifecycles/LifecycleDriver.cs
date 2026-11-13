// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Semantics;
using Microsoft.Extensions.Logging;

namespace Cratis.Stage.Lifecycles;

/// <summary>
/// Drives the state machines and workflows of a model from the facts the semantic world commits.
/// </summary>
/// <remarks>
/// A state machine is derived from the facts with the reference evaluator, so the runtime takes exactly the transitions the
/// specifications pin. When a fact moves an instance into a state, the entering actions run: a command is sent, a workflow is
/// started. Workflows start on the event their trigger names or on the state their trigger waits to see entered, and a fact that a
/// wait node waits for resumes the runs holding it. Only facts committed after the driver started act; history is never replayed
/// into side effects.
/// </remarks>
/// <param name="plan">The execution plan of the loaded model.</param>
/// <param name="runtime">The runtime whose committed facts drive the lifecycles.</param>
/// <param name="facts">Reads the committed facts.</param>
/// <param name="effects">Performs what entering a state or a fact asks for.</param>
/// <param name="executableWorkflows">The names of the workflows the runtime drives.</param>
/// <param name="logger">The logger.</param>
public sealed class LifecycleDriver(SemanticExecutionPlan plan, ISemanticRuntime runtime, ISemanticWorldFacts facts, ILifecycleEffects effects, IReadOnlySet<string> executableWorkflows, ILogger logger) : IDisposable
{
    readonly SemaphoreSlim _pumping = new(1, 1);
    IDisposable? _subscription;
    int _processed;

    /// <inheritdoc/>
    public void Dispose()
    {
        _subscription?.Dispose();
        _pumping.Dispose();
    }

    /// <summary>
    /// Starts acting on the facts committed from now on.
    /// </summary>
    /// <returns>A task that completes when the driver is listening.</returns>
    public async Task Start()
    {
        _processed = (await facts.Facts()).Length;
        if (runtime is ISemanticWorldChanges changes)
        {
            _subscription = changes.Subscribe(() => _ = Task.Run(Pump));
        }
    }

    /// <summary>
    /// Acts on every fact committed since the last pass.
    /// </summary>
    /// <returns>A task that completes when the facts were acted on.</returns>
    public async Task Pump()
    {
        await _pumping.WaitAsync();
        try
        {
            var committed = await facts.Facts();
            for (; _processed < committed.Length; _processed++)
            {
                try
                {
                    await Act(committed, _processed);
                }
                catch (Exception exception)
                {
                    LifecycleLog.FactFailed(logger, exception, _processed);
                }
            }
        }
        finally
        {
            _pumping.Release();
        }
    }

    /// <summary>
    /// Gets where every instance of a state machine is, derived from the committed facts.
    /// </summary>
    /// <param name="machine">The state machine.</param>
    /// <returns>The key and state of each instance.</returns>
    public async Task<ImmutableArray<(SemanticValue Key, string State)>> States(SemanticStateMachine machine)
    {
        var committed = await facts.Facts();
        var keys = new List<SemanticValue>();
        foreach (var fact in committed.Where(fact => machine.Transitions.Any(transition => transition.Event == fact.EventContract)))
        {
            if (!keys.Exists(key => Equals(key, fact.Destination))) keys.Add(fact.Destination);
        }

        return [.. keys.Select(key => (key, SemanticLifecycleRunner.Derive(plan, machine, committed, key).State))];
    }

    static bool Starts(SemanticWorkflowNode node, SemanticFact fact, List<EnteredState> entered) =>
        node.Kind == SemanticWorkflowNodeKind.Trigger && node.TriggerKind switch
        {
            SemanticWorkflowTriggerKind.Event => node.Event == fact.EventContract,
            SemanticWorkflowTriggerKind.StateEntered => entered.Exists(item => item.Machine.Name == node.StateMachine && item.State == node.State),
            _ => false
        };

    async Task Enter(StateEntryInvocation entry, Func<SemanticId, Task<ImmutableArray<SemanticPropertyValue>>> subject)
    {
        var effect = entry.Effect;
        switch (effect.Kind)
        {
            case SemanticStateEntryKind.SendCommand:
                await effects.Send(plan.Commands[effect.Command], entry.Key, await subject(entry.Machine.ReadModel));
                break;
            case SemanticStateEntryKind.InvokeAbility:
                await effects.Invoke(entry, await subject(entry.Machine.ReadModel));
                break;
            default:
                if (plan.Model.Application.Workflows.FirstOrDefault(workflow => workflow.Name == effect.Workflow) is { } started && executableWorkflows.Contains(started.Name))
                {
                    await effects.Start(started, entry.Key, entry.Causation, await subject(started.Subject));
                }

                break;
        }
    }

    async Task Act(ImmutableArray<SemanticFact> committed, int index)
    {
        var fact = committed[index];
        var entered = Entered(committed, index);
        var subjects = new Dictionary<SemanticId, ImmutableArray<SemanticPropertyValue>>();
        async Task<ImmutableArray<SemanticPropertyValue>> Subject(SemanticId readModel)
        {
            if (subjects.TryGetValue(readModel, out var known)) return known;
            var instances = await runtime.ReadModels(readModel);
            var instance = instances.FirstOrDefault(candidate => Equals(candidate.Key, fact.Destination));
            return subjects[readModel] = instance?.Values ?? [];
        }

        foreach (var (machine, state, entering) in entered)
        {
            for (var position = 0; position < entering.Length; position++)
            {
                await Enter(new(machine, state, position + 1, entering[position], fact.Destination, $"entered:{machine.Name}:{state}:{index}"), Subject);
            }
        }

        foreach (var workflow in plan.Model.Application.Workflows.Where(workflow => executableWorkflows.Contains(workflow.Name)))
        {
            foreach (var node in workflow.Nodes)
            {
                await Observe(workflow, node, fact, index, entered, Subject);
            }
        }
    }

    List<EnteredState> Entered(ImmutableArray<SemanticFact> committed, int index)
    {
        var fact = committed[index];
        var entered = new List<EnteredState>();
        foreach (var machine in plan.Model.Application.StateMachines.Where(machine => machine.Transitions.Any(transition => transition.Event == fact.EventContract)))
        {
            var previous = SemanticLifecycleRunner.Derive(plan, machine, committed[..index], fact.Destination).State;
            var (state, entering) = SemanticLifecycleRunner.Derive(plan, machine, committed[..(index + 1)], fact.Destination);
            if (state != previous) entered.Add(new(machine, state, entering));
        }

        return entered;
    }

    async Task Observe(
        SemanticWorkflow workflow,
        SemanticWorkflowNode node,
        SemanticFact fact,
        int index,
        List<EnteredState> entered,
        Func<SemanticId, Task<ImmutableArray<SemanticPropertyValue>>> subject)
    {
        if (Starts(node, fact, entered))
        {
            // A state entered by a machine that also starts this workflow shares the dedup key, so the run starts once.
            var enteredBy = node.TriggerKind == SemanticWorkflowTriggerKind.StateEntered ? $"entered:{node.StateMachine}:{node.State}:{index}" : $"event:{index}";
            await effects.Start(workflow, fact.Destination, enteredBy, await subject(workflow.Subject));
        }
        else if (node.Kind == SemanticWorkflowNodeKind.Wait && node.Event == fact.EventContract)
        {
            await effects.Resume(workflow, node.Name, fact.Destination);
        }
    }

    sealed record EnteredState(SemanticStateMachine Machine, string State, ImmutableArray<SemanticStateEntry> Effects);
}
