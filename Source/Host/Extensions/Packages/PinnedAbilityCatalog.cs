// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Stage.Host.Invocations;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Cratis.Stage.Host.Extensions.Packages;

/// <summary>
/// Gives every ability of an extension the locked digest of its package, so every workflow run pins exactly the package that was admitted, and upgrading the lock never
/// changes a run in flight. A system that names no loaded extension keeps the digest its descriptor has.
/// </summary>
/// <param name="inner">The <see cref="IAbilityCatalog"/> that describes the abilities.</param>
/// <param name="packages">The <see cref="IExtensionPackages"/>.</param>
public sealed class PinnedAbilityCatalog(IAbilityCatalog inner, IExtensionPackages packages) : IAbilityCatalog
{
    /// <inheritdoc/>
    public IReadOnlyCollection<AbilityDescriptor> All => [.. inner.All.Select(Pin)];

    /// <summary>
    /// Registers the packages and pins every ability the catalog describes to them.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/>.</param>
    /// <param name="packages">The <see cref="IExtensionPackages"/>.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddExtensionPackages(IServiceCollection services, IExtensionPackages packages)
    {
        services.AddSingleton(packages);
        var previous = services.LastOrDefault(descriptor => descriptor.ServiceType == typeof(IAbilityCatalog));
        services.Replace(ServiceDescriptor.Singleton<IAbilityCatalog>(provider => new PinnedAbilityCatalog(Previous(previous, provider), packages)));
        return services;
    }

    /// <inheritdoc/>
    public AbilityDescriptor? Find(string system, string ability) => inner.Find(system, ability) is { } descriptor ? Pin(descriptor) : null;

    static IAbilityCatalog Previous(ServiceDescriptor? descriptor, IServiceProvider provider) => descriptor switch
    {
        null => AbilityCatalog.Empty,
        { ImplementationInstance: IAbilityCatalog instance } => instance,
        { ImplementationFactory: { } factory } => (IAbilityCatalog)factory(provider),
        { ImplementationType: { } type } => (IAbilityCatalog)ActivatorUtilities.CreateInstance(provider, type),
        _ => AbilityCatalog.Empty
    };

    AbilityDescriptor Pin(AbilityDescriptor descriptor) =>
        packages.ForSystem(descriptor.Reference.System) is { } package ? descriptor with { PackageDigest = package.Digest } : descriptor;
}
