// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Host.Extensions.Components;

/// <summary>
/// Knows which extension components the loaded model places on which screens. This is the seam the language support for extension components fills.
/// </summary>
public interface IExtensionComponentCatalog
{
    /// <summary>
    /// Gets every placement.
    /// </summary>
    IReadOnlyCollection<ExtensionComponentPlacement> All { get; }

    /// <summary>
    /// Finds a placement.
    /// </summary>
    /// <param name="screen">The name of the screen.</param>
    /// <param name="system">The name of the system.</param>
    /// <param name="component">The name of the component.</param>
    /// <returns>The placement, or null when the screen places no such component.</returns>
    ExtensionComponentPlacement? Find(string screen, string system, string component);
}
