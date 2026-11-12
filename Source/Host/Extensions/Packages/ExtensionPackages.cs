// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
