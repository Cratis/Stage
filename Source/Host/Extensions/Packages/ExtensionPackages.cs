// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;
using Cratis.Stage.Host.Invocations;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Cratis.Stage.Host.Extensions.Packages;

/// <summary>
/// The loaded packages, by the system of the model that names them. This is what activation builds on: it says which package a system's backend is, where its verified
/// bytes are and what digest every run of its abilities pins.
/// </summary>
public interface IExtensionPackages
{
    /// <summary>
    /// Gets everything that was loaded and everything that was refused.
    /// </summary>
    ExtensionPackageSet Set { get; }

    /// <summary>
    /// Finds the package a system's extension is.
    /// </summary>
    /// <param name="system">The name of the system.</param>
    /// <returns>The <see cref="LoadedExtensionPackage"/>, or null when the system names no loaded extension.</returns>
    LoadedExtensionPackage? ForSystem(string system);
}

/// <summary>
/// The <see cref="IExtensionPackages"/> of a model.
/// </summary>
/// <param name="set">The <see cref="ExtensionPackageSet"/>.</param>
/// <param name="references">The extension reference each system of the model names.</param>
public sealed class ExtensionPackages(ExtensionPackageSet set, IReadOnlyDictionary<string, string> references) : IExtensionPackages
{
    /// <inheritdoc/>
    public ExtensionPackageSet Set => set;

    /// <inheritdoc/>
    public LoadedExtensionPackage? ForSystem(string system) => references.TryGetValue(system, out var reference) ? set.FindByReference(reference) : null;
}

/// <summary>
/// Reads which extension each system of a model names, from the model's <c lang="text">.play</c> files.
/// </summary>
public static partial class SystemExtensionReferences
{
    /// <summary>
    /// Reads the references.
    /// </summary>
    /// <param name="modelPath">The model's folder or one of its files.</param>
    /// <returns>The extension reference by system name.</returns>
    public static IReadOnlyDictionary<string, string> Read(string modelPath)
    {
        var files = Directory.Exists(modelPath)
            ? Directory.EnumerateFiles(modelPath, "*.play", SearchOption.AllDirectories).Where(file => !file.Contains($"{Path.DirectorySeparatorChar}.", StringComparison.Ordinal))
            : File.Exists(modelPath) ? [modelPath] : [];
        var references = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var file in files) Read(File.ReadLines(file), references);
        return references;
    }

    /// <summary>
    /// Reads the references from the lines of one file.
    /// </summary>
    /// <param name="lines">The lines.</param>
    /// <param name="references">Receives the extension reference by system name.</param>
    public static void Read(IEnumerable<string> lines, Dictionary<string, string> references)
    {
        string? system = null;
        foreach (var line in lines)
        {
            if (line.Trim().Length == 0 || line.TrimStart().StartsWith('#')) continue;

            if (!char.IsWhiteSpace(line[0]))
            {
                system = SystemLine().Match(line) is { Success: true } header ? header.Groups[1].Value : null;
            }
            else if (system is not null && ExtensionLine().Match(line) is { Success: true } extension)
            {
                references[system] = extension.Groups[1].Value;
            }
        }
    }

    [GeneratedRegex(@"^system\s+([A-Za-z_][A-Za-z0-9_]*)\s*$")]
    private static partial Regex SystemLine();

    [GeneratedRegex("^\\s+extension\\s+\"([^\"]+)\"")]
    private static partial Regex ExtensionLine();
}

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

    /// <inheritdoc/>
    public AbilityDescriptor? Find(string system, string ability) => inner.Find(system, ability) is { } descriptor ? Pin(descriptor) : null;

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
