// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Host.Invocations;

/// <summary>
/// Realizes abilities: the extension protocol sits behind it. The runtime knows nothing about how an extension talks to its system, only what
/// it answers for an attempt.
/// </summary>
public interface IAbilityBackend
{
    /// <summary>
    /// Performs one attempt at an ability.
    /// </summary>
    /// <param name="call">The <see cref="AbilityCall"/>.</param>
    /// <param name="cancellationToken">Cancelled when the attempt runs out of time.</param>
    /// <returns>The <see cref="AbilityResult"/>.</returns>
    Task<AbilityResult> Invoke(AbilityCall call, CancellationToken cancellationToken);
}
