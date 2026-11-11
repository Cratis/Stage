// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Cratis.Stage.Host.Invocations;

/// <summary>
/// Decides whether an invocation may run with the ledger the host has. The guard that keeps a <see cref="EffectClass.Once"/> effect from repeating
/// is a record that an attempt began, and it only protects against a restart when the record survives it.
/// </summary>
/// <param name="options">The invocation options.</param>
[IgnoreConvention]
public sealed class InvocationDurability(IOptions<StageInvocationOptions> options)
{
    /// <summary>
    /// Gets why an ability may not run with this ledger.
    /// </summary>
    /// <param name="descriptor">The ability.</param>
    /// <returns>The reason, or null when the ability may run.</returns>
    public string? Refusal(AbilityDescriptor descriptor) =>
        descriptor.Effect != EffectClass.Once || options.Value.GrainStorageIsDurable || options.Value.AllowVolatileOnce
            ? null
            : $"{descriptor.Reference.Capability} happens at most once, and this host keeps its invocation ledger in memory, which a restart loses. " +
              "Set Stage:Orleans:Storage to MongoDB, or set Stage:Invocations:AllowVolatileOnce to true to accept that a restart can repeat the effect.";
}
