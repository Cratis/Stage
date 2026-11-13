// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Contracts.Lifecycles;
using Cratis.Stage.Host.Invocations;
using Cratis.Stage.Lifecycles;

namespace Cratis.Stage.Host.Lifecycles;

/// <summary>
/// Invokes the abilities state machines invoke on entering a state, through the invocation ledger.
/// </summary>
/// <remarks>
/// The origin names the entry and what entered it, so delivering the same entering again finds the invocation it already recorded rather than
/// invoking the ability twice.
/// </remarks>
/// <param name="plan">The execution plan of the loaded model.</param>
/// <param name="invocations">Resolves the <see cref="IAbilityInvocations"/> when an entry first invokes an ability; it gives null when the host runs no abilities.</param>
internal sealed class StateEntryAbilities(SemanticExecutionPlan plan, Func<IAbilityInvocations?> invocations)
{
    /// <summary>
    /// Invokes the ability of a state entry.
    /// </summary>
    /// <param name="invocation">The state entry that invokes the ability.</param>
    /// <param name="subject">The current values of the followed read model instance.</param>
    /// <returns>The invocation the ledger admitted.</returns>
    /// <exception cref="InvalidOperationException">The ability is not declared, its input cannot be composed, or the ledger refused it.</exception>
    internal async Task<InvocationRecord?> Invoke(StateEntryInvocation invocation, ImmutableArray<SemanticPropertyValue> subject)
    {
        var effect = invocation.Effect;
        var where = $"Entering '{invocation.State}' of state machine '{invocation.Machine.Name}'";
        if (AbilityInputs.Find(plan.Model.Application, effect.System, effect.Ability) is not ({ } system, { } ability))
        {
            throw new InvalidOperationException($"{where} invokes the ability '{effect.Ability}', which no system of the model declares.");
        }

        var inputs = AbilityInputs.Describe(effect.Inputs)
            ?? throw new InvalidOperationException($"{where} maps an input of '{system.Name}.{ability.Name}' the runtime cannot compose.");
        if (invocations() is not { } ledger)
        {
            throw new InvalidOperationException($"{where} invokes '{system.Name}.{ability.Name}', but the host runs no abilities.");
        }

        var key = LifecycleSubjects.KeyText(invocation.Key);
        var input = AbilityInputs.Compose(inputs, LifecycleSubjects.SubjectJson(plan, invocation.Machine.ReadModel, invocation.Key, subject), null);
        var origin = new InvocationOrigin(InvocationOriginKind.StateMachineEntry, invocation.Address, $"{key}:{invocation.Causation}");
        var admission = await ledger.Invoke(new(system.Name, ability.Name, origin, input.ToJsonString()));
        return admission.Admitted
            ? admission.Record
            : throw new InvalidOperationException($"{where} invoked '{system.Name}.{ability.Name}' for {key}, which was refused: {admission.Refusal}");
    }
}
