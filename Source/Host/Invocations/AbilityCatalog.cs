// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Host.Invocations;

/// <summary>
/// An <see cref="IAbilityCatalog"/> over a fixed set of descriptors.
/// </summary>
/// <param name="abilities">The descriptors.</param>
public sealed class AbilityCatalog(IEnumerable<AbilityDescriptor> abilities) : IAbilityCatalog
{
    readonly Dictionary<(string System, string Ability), AbilityDescriptor> _abilities =
        abilities.ToDictionary(descriptor => (descriptor.Reference.System, descriptor.Reference.Ability));

    /// <summary>
    /// Gets a catalog without any ability, which is what a model without systems has.
    /// </summary>
    public static AbilityCatalog Empty { get; } = new([]);

    /// <inheritdoc/>
    public IReadOnlyCollection<AbilityDescriptor> All => _abilities.Values;

    /// <inheritdoc/>
    public AbilityDescriptor? Find(string system, string ability) => _abilities.GetValueOrDefault((system, ability));
}
