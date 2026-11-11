// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Stage.Host.Invocations;

namespace Cratis.Stage.Host.Extensions;

/// <summary>
/// Checks a running backend against the contract of its system, which is what the handshake is for.
/// </summary>
public static class ExtensionContract
{
    /// <summary>
    /// Finds how a backend departs from the abilities its system declares. A backend may realize more than the contract says; it may not realize less
    /// or differently, because a model that invokes an ability depends on its major, its effect class and its outcomes.
    /// </summary>
    /// <param name="declared">The abilities the system declares.</param>
    /// <param name="described">What the backend said.</param>
    /// <returns>The departures, empty when the backend honors the contract.</returns>
    public static IReadOnlyList<string> Departures(IEnumerable<AbilityDescriptor> declared, ExtensionDescription described)
    {
        var departures = new List<string>();
        foreach (var ability in declared)
        {
            var name = ability.Reference.Ability;
            var realized = described.Abilities.FirstOrDefault(candidate => candidate.Name == name && candidate.ContractMajor == ability.Reference.ContractMajor);
            if (realized is null)
            {
                departures.Add($"{ability.Reference.Capability} is not realized.");
                continue;
            }

            if (Effect(realized.Effect) != ability.Effect)
            {
                departures.Add($"{ability.Reference.Capability} is declared {ability.Effect} and realized as {Effect(realized.Effect)}.");
            }

            if (!realized.Outcomes.Order(StringComparer.Ordinal).SequenceEqual(ability.Outcomes.Order(StringComparer.Ordinal)))
            {
                departures.Add($"{ability.Reference.Capability} declares the outcomes [{string.Join(", ", ability.Outcomes)}] and the backend realizes [{string.Join(", ", realized.Outcomes)}].");
            }
        }

        return departures;
    }

    /// <summary>
    /// Maps the effect class of the protocol to the runtime's.
    /// </summary>
    /// <param name="effect">The protocol's effect class.</param>
    /// <returns>The runtime's <see cref="EffectClass"/>. An unspecified class is the strictest, once.</returns>
    public static EffectClass Effect(Cratis.Stage.Extensions.Protocol.EffectClass effect) => effect switch
    {
        Cratis.Stage.Extensions.Protocol.EffectClass.Safe => EffectClass.Safe,
        Cratis.Stage.Extensions.Protocol.EffectClass.Idempotent => EffectClass.Idempotent,
        _ => EffectClass.Once
    };

    /// <summary>
    /// Maps the runtime's effect class to the protocol's.
    /// </summary>
    /// <param name="effect">The runtime's effect class.</param>
    /// <returns>The protocol's effect class.</returns>
    public static Cratis.Stage.Extensions.Protocol.EffectClass Effect(EffectClass effect) => effect switch
    {
        EffectClass.Safe => Cratis.Stage.Extensions.Protocol.EffectClass.Safe,
        EffectClass.Idempotent => Cratis.Stage.Extensions.Protocol.EffectClass.Idempotent,
        _ => Cratis.Stage.Extensions.Protocol.EffectClass.Once
    };
}
