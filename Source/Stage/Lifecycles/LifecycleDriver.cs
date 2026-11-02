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
internal sealed class LifecycleDriver : IDisposable
{
    readonly SemanticExecutionPlan _plan;
    readonly ISemanticRuntime _runtime;
    readonly ISemanticWorldFacts _facts;
    readonly ILifecycleEffects _effects;
    readonly IReadOnlySet<string> _executable;
    readonly ILogger _logger;
    readonly SemaphoreSlim _pumping = new(1, 1);
    IDisposable? _subscription;
    int _processed;

    internal LifecycleDriver(SemanticExecutionPlan plan, ISemanticRuntime runtime, ISemanticWorldFacts facts, ILifecycleEffects effects, IReadOnlySet<string> executableWorkflows, ILogger logger)
    {
        _plan = plan;
        _runtime = runtime;
        _facts = facts;
        _effects = effects;
        _executable = executableWorkflows;
        _logger = logger;
    }

    /// <summary>
    /// Starts acting on the facts committed from now on.
    /// </summary>
    /// <returns>A task that completes when the driver is listening.</returns>
    internal async Task Start()
    {
        _processed = (await _facts.Facts()).Length;
        if (_runtime is ISemanticWorldChanges changes)
        {
            _subscription = changes.Subscribe(() => _ = Task.Run(Pump));
        }
    }

    /// <summary>
    /// Acts on every fact committed since the last pass.
    /// </summary>
    /// <returns>A task that completes when the facts were acted on.</returns>
    internal async Task Pump()
    {
        await _pumping.WaitAsync();
        try
        {
            var facts = await _facts.Facts();
            for (; _processed < facts.Length; _processed++)
            {
                try
                {
                    await Act(facts, _processed);
                }
                catch (Exception exception)
                {
                    LifecycleLog.FactFailed(_logger, exception, _processed);
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
    internal async Task<ImmutableArray<(SemanticValue Key, string State)>> States(SemanticStateMachine machine)
    {
        var facts = await _facts.Facts();
        var keys = new List<SemanticValue>();
        foreach (var fact in facts.Where(fact => machine.Transitions.Any(transition => transition.Event == fact.EventContract)))
        {
            if (!keys.Exists(key => Equals(key, fact.Destination))) keys.Add(fact.Destination);
        }

        return [.. keys.Select(key => (key, SemanticLifecycleRunner.Derive(_plan, machine, facts, key).State))];
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _subscription?.Dispose();
        _pumping.Dispose();
    }

    async Task Act(ImmutableArray<SemanticFact> facts, int index)
    {
        var fact = facts[index];
        var before = facts[..index];
        var after = facts[..(index + 1)];
        var application = _plan.Model.Application;
        var entered = new List<(SemanticStateMachine Machine, string State, ImmutableArray<SemanticStateEntry> Effects)>();
        foreach (var machine in application.StateMachines.Where(machine => machine.Transitions.Any(transition => transition.Event == fact.EventContract)))
        {
            var previous = SemanticLifecycleRunner.Derive(_plan, machine, before, fact.Destination).State;
            var (state, effects) = SemanticLifecycleRunner.Derive(_plan, machine, after, fact.Destination);
            if (state != previous) entered.Add((machine, state, effects));
        }

        var subjects = new Dictionary<SemanticId, ImmutableArray<SemanticPropertyValue>>();
        async Task<ImmutableArray<SemanticPropertyValue>> Subject(SemanticId readModel)
        {
            if (subjects.TryGetValue(readModel, out var known)) return known;
            var instances = await _runtime.ReadModels(readModel);
            var instance = instances.FirstOrDefault(candidate => Equals(candidate.Key, fact.Destination));
            return subjects[readModel] = instance?.Values ?? [];
        }

        foreach (var (machine, state, effects) in entered)
        {
            foreach (var effect in effects)
            {
                if (effect.Kind == SemanticStateEntryKind.SendCommand)
                {
                    await _effects.Send(_plan.Commands[effect.Command], fact.Destination, await Subject(machine.ReadModel));
                }
                else if (application.Workflows.FirstOrDefault(workflow => workflow.Name == effect.Workflow) is { } started && _executable.Contains(started.Name))
                {
                    await _effects.Start(started, fact.Destination, $"entered:{machine.Name}:{state}:{index}", await Subject(started.Subject));
                }
            }
        }

        foreach (var workflow in application.Workflows.Where(workflow => _executable.Contains(workflow.Name)))
        {
            foreach (var node in workflow.Nodes)
            {
                var starts = node.Kind == SemanticWorkflowNodeKind.Trigger &&
                    ((node.TriggerKind == SemanticWorkflowTriggerKind.Event && node.Event == fact.EventContract) ||
                     (node.TriggerKind == SemanticWorkflowTriggerKind.StateEntered && entered.Exists(item => item.Machine.Name == node.StateMachine && item.State == node.State)));
                if (starts)
                {
                    // A state entered by a machine that also starts this workflow shares the dedup key, so the run starts once.
                    var enteredBy = node.TriggerKind == SemanticWorkflowTriggerKind.StateEntered ? $"entered:{node.StateMachine}:{node.State}:{index}" : $"event:{index}";
                    await _effects.Start(workflow, fact.Destination, enteredBy, await Subject(workflow.Subject));
                }
                else if (node.Kind == SemanticWorkflowNodeKind.Wait && node.Event == fact.EventContract)
                {
                    await _effects.Resume(workflow, node.Name, fact.Destination);
                }
            }
        }
    }
}
