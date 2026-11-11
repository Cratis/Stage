// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Choreography.Workflows.Adapters;
using Cratis.Choreography.Workflows.Graphs;
using Cratis.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Cratis.Stage.Host.Invocations;

/// <summary>
/// Finds the performer of an ability capability, <c lang="text">ability:&lt;System&gt;.&lt;Ability&gt;@&lt;contract major&gt;</c>, and of the settlement node that follows it.
/// A capability at a contract major the model does not declare has no performer, so the engine fails the node instead of calling an extension built for another contract.
/// </summary>
[IgnoreConvention]
internal sealed class AbilityWorkflowPerformers : IWorkflowNodePerformers
{
    readonly Dictionary<string, AbilityPerformer> _abilities;
    readonly SettlePerformer _settle;

    /// <summary>
    /// Initializes a new instance of the <see cref="AbilityWorkflowPerformers"/> class.
    /// </summary>
    /// <param name="catalog">The <see cref="IAbilityCatalog"/>.</param>
    /// <param name="ledger">The <see cref="IInvocationLedger"/>.</param>
    /// <param name="backend">The <see cref="IAbilityBackend"/>.</param>
    /// <param name="gates">The <see cref="SystemGates"/>.</param>
    /// <param name="durability">The <see cref="InvocationDurability"/>.</param>
    /// <param name="settlements">The <see cref="InvocationSettlements"/>.</param>
    /// <param name="loggers">The logger factory.</param>
    public AbilityWorkflowPerformers(IAbilityCatalog catalog, IInvocationLedger ledger, IAbilityBackend backend, SystemGates gates, InvocationDurability durability, InvocationSettlements settlements, ILoggerFactory loggers)
    {
        var logger = loggers.CreateLogger("Cratis.Stage.Invocations");
        _abilities = catalog.All.ToDictionary(
            descriptor => descriptor.Reference.Capability,
            descriptor => new AbilityPerformer(descriptor, ledger, backend, gates, durability, logger),
            StringComparer.Ordinal);
        _settle = new(ledger, settlements);
    }

    /// <inheritdoc/>
    public IWorkflowNodePerformer? For(CapabilityReference capability) =>
        capability.Id.Value == SettlePerformer.CapabilityId
            ? _settle
            : _abilities.GetValueOrDefault($"{capability.Id.Value}@{capability.Version.Value}");
}
