// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Host.Extensions.Components;

/// <summary>
/// An <see cref="IExtensionComponentCatalog"/> over a fixed set of placements.
/// </summary>
/// <param name="placements">The placements.</param>
public sealed class ExtensionComponentCatalog(IEnumerable<ExtensionComponentPlacement> placements) : IExtensionComponentCatalog
{
    /// <summary>
    /// Gets a catalog that places nothing, which is what a model without extension components gets.
    /// </summary>
    public static readonly ExtensionComponentCatalog Empty = new([]);

    readonly List<ExtensionComponentPlacement> _placements = [.. placements];

    /// <inheritdoc/>
    public IReadOnlyCollection<ExtensionComponentPlacement> All => _placements;

    /// <inheritdoc/>
    public ExtensionComponentPlacement? Find(string screen, string system, string component) =>
        _placements.FirstOrDefault(placement =>
            placement.Screen == screen && placement.System == system && placement.Component == component);
}
