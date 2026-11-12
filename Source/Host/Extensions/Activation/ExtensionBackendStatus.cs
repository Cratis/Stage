// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Host.Extensions.Activation;

/// <summary>
/// What the Workbench shows of one extension's backend.
/// </summary>
/// <param name="System">The name of the system.</param>
/// <param name="Reference">The reference of the package.</param>
/// <param name="Tier">How the backend runs.</param>
/// <param name="State">Where it is in its life.</param>
/// <param name="Activation">When it runs, as <c lang="text">always</c> or <c lang="text">on demand, idle after hh:mm:ss</c>.</param>
/// <param name="LastActivation">When it was last started.</param>
/// <param name="LastStopped">When it was last stopped.</param>
/// <param name="InFlight">How many calls are in flight.</param>
/// <param name="Refusal">Why it could not be started the last time, or empty.</param>
public sealed record ExtensionBackendStatus(
    string System,
    string Reference,
    ExecutionTier Tier,
    BackendState State,
    string Activation,
    DateTimeOffset? LastActivation,
    DateTimeOffset? LastStopped,
    int InFlight,
    string Refusal)
{
    /// <summary>
    /// Describes a slot.
    /// </summary>
    /// <param name="slot">The <see cref="BackendSlot"/>.</param>
    /// <returns>The <see cref="ExtensionBackendStatus"/>.</returns>
    public static ExtensionBackendStatus From(BackendSlot slot) => new(
        slot.System,
        slot.Package.Lock.Reference,
        slot.Plan.Tier,
        slot.State,
        slot.Policy.ToString(),
        slot.LastActivation,
        slot.LastStopped,
        slot.InFlight,
        slot.Refusal.Length > 0 ? slot.Refusal : slot.Plan.Refusal);
}
