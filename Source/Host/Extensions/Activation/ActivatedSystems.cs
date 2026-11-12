// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Stage.Host.Extensions.Packages;
using Microsoft.Extensions.Options;

namespace Cratis.Stage.Host.Extensions.Activation;

/// <summary>
/// The consumer's <c lang="text">activation</c> lines, by system.
/// </summary>
public interface IActivationOverrides
{
    /// <summary>
    /// Finds the override of a system.
    /// </summary>
    /// <param name="system">The name of the system.</param>
    /// <returns>The <see cref="ActivationOverride"/>, or null when the system states none.</returns>
    ActivationOverride? For(string system);
}

/// <summary>
/// The <see cref="IActivationOverrides"/> read from a model.
/// </summary>
/// <param name="overrides">The overrides by system name.</param>
public sealed class ModelActivationOverrides(IReadOnlyDictionary<string, ActivationOverride> overrides) : IActivationOverrides
{
    /// <inheritdoc/>
    public ActivationOverride? For(string system) => overrides.GetValueOrDefault(system);
}

/// <summary>
/// The systems whose backend Stage starts and stops, each with the tier and policy settled from its contract, the deployment and the consumer's override.
/// </summary>
/// <param name="packages">The <see cref="IExtensionPackages"/> of the model, when it has any.</param>
/// <param name="overrides">The <see cref="IActivationOverrides"/> of the model, when it has any.</param>
/// <param name="options">The <see cref="ExtensionOptions"/>.</param>
public sealed class ActivatedSystems(IEnumerable<IExtensionPackages> packages, IEnumerable<IActivationOverrides> overrides, IOptions<ExtensionOptions> options)
{
    readonly Lazy<IReadOnlyDictionary<string, BackendSlot>> _slots = new(() => Build(packages.FirstOrDefault(), overrides.FirstOrDefault(), options.Value.Execution));

    /// <summary>
    /// Gets every system that names an extension Stage has loaded.
    /// </summary>
    public IReadOnlyCollection<BackendSlot> All => [.. _slots.Value.Values];

    /// <summary>
    /// Finds a system.
    /// </summary>
    /// <param name="system">The name of the system.</param>
    /// <returns>The <see cref="BackendSlot"/>, or null when the system names no loaded extension.</returns>
    public BackendSlot? Find(string system) => _slots.Value.GetValueOrDefault(system);

    static Dictionary<string, BackendSlot> Build(IExtensionPackages? packages, IActivationOverrides? overrides, ExecutionOptions execution)
    {
        var slots = new Dictionary<string, BackendSlot>(StringComparer.OrdinalIgnoreCase);
        if (packages is null) return slots;

        var planner = new ExecutionTierPlanner(execution);
        foreach (var system in packages.Systems)
        {
            if (packages.ForSystem(system) is not { } package) continue;
            var policy = ActivationPolicy.Resolve(package.Contract.Activation, overrides?.For(system), execution.IdleTimeout);
            slots[system] = new BackendSlot(system, package, planner.Plan(system, package), policy);
        }

        return slots;
    }
}
