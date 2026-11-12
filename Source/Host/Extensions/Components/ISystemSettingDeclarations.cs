// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Host.Extensions.Components;

/// <summary>
/// Knows the settings the loaded model's systems declare. This is the seam the language support for settings fills.
/// </summary>
public interface ISystemSettingDeclarations
{
    /// <summary>
    /// Gets the settings one system declares.
    /// </summary>
    /// <param name="system">The name of the system.</param>
    /// <returns>The declarations, empty when the system declares none.</returns>
    IReadOnlyList<SettingDeclaration> Of(string system);
}
