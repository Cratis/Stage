// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Host.Invocations;

/// <summary>
/// The deployment configuration of ability invocations, bound from <c lang="text">Stage:Invocations</c>.
/// </summary>
public sealed class StageInvocationOptions
{
    /// <summary>
    /// Gets the configuration section the options are bound from.
    /// </summary>
    public const string Section = "Stage:Invocations";

    /// <summary>
    /// Gets or sets whether the silo's grain storage survives a restart of the process. It is set from <c lang="text">Stage:Orleans:Storage</c>, and only MongoDB does.
    /// </summary>
    public bool GrainStorageIsDurable { get; set; }

    /// <summary>
    /// Gets or sets whether a <see cref="EffectClass.Once"/> ability may run although the ledger is lost with the process. A restart in the middle of an
    /// attempt can then repeat the effect. It is meant for a sandbox, where nothing outside the session can be hurt.
    /// </summary>
    public bool AllowVolatileOnce { get; set; }

    /// <summary>
    /// Gets the limits per system, by the name of the system.
    /// </summary>
    public IDictionary<string, SystemGateOptions> Systems { get; } = new Dictionary<string, SystemGateOptions>(StringComparer.Ordinal);
}

/// <summary>
/// The limits of one system. A system that is slow or failing fills its own gate and nothing else.
/// </summary>
public sealed class SystemGateOptions
{
    /// <summary>
    /// Gets or sets how many calls to the system may be in flight at once on a silo. Each silo has its own gate, so divide the allowance by the number of silos.
    /// Not limited when null.
    /// </summary>
    public int? MaxConcurrent { get; set; }

    /// <summary>
    /// Gets or sets how many calls per second a silo may make to the system. Not limited when null.
    /// </summary>
    public double? RequestsPerSecond { get; set; }

    /// <summary>
    /// Gets or sets the attempts every ability of the system may use, narrowing what the ability declares. Not narrowed when null.
    /// </summary>
    public int? MaxAttempts { get; set; }
}
