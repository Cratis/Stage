// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Host.Extensions.Http;

/// <summary>
/// The abilities realized by an <c lang="text">http</c> block.
/// </summary>
public interface IHttpAbilityDefinitions
{
    /// <summary>
    /// Finds the realization of an ability.
    /// </summary>
    /// <param name="system">The name of the system.</param>
    /// <param name="ability">The name of the ability.</param>
    /// <returns>The <see cref="HttpAbilityDefinition"/>, or null when the ability is not realized by <c lang="text">http</c>.</returns>
    HttpAbilityDefinition? Find(string system, string ability);
}
