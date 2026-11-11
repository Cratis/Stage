// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Host.Invocations;

/// <summary>
/// Knows the abilities the loaded model's systems declare. This is the seam the language support for abilities fills: the binder's
/// semantic model of a system gives one <see cref="AbilityDescriptor"/> per ability, and the runtime needs nothing else from it.
/// </summary>
public interface IAbilityCatalog
{
    /// <summary>
    /// Gets every ability.
    /// </summary>
    IReadOnlyCollection<AbilityDescriptor> All { get; }

    /// <summary>
    /// Finds an ability.
    /// </summary>
    /// <param name="system">The name of the system.</param>
    /// <param name="ability">The name of the ability.</param>
    /// <returns>The descriptor, or null when the system declares no such ability.</returns>
    AbilityDescriptor? Find(string system, string ability);
}
