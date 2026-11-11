// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Host.Invocations;

/// <summary>
/// The backend of a host without extension packages loaded: nothing can be reached, so every attempt answers that it was not sent.
/// </summary>
public sealed class NoAbilityBackend : IAbilityBackend
{
    /// <inheritdoc/>
    public Task<AbilityResult> Invoke(AbilityCall call, CancellationToken cancellationToken) =>
        Task.FromResult<AbilityResult>(new AbilityNotSent($"No extension is loaded that realizes {call.Ability.Capability}."));
}
