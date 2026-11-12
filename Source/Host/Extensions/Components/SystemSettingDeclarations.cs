// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Host.Extensions.Components;

/// <summary>
/// An <see cref="ISystemSettingDeclarations"/> over a fixed set of declarations.
/// </summary>
/// <param name="declarations">The declarations.</param>
public sealed class SystemSettingDeclarations(IEnumerable<SettingDeclaration> declarations) : ISystemSettingDeclarations
{
    /// <summary>
    /// Gets declarations that declare nothing, which is what a model without systems gets.
    /// </summary>
    public static readonly SystemSettingDeclarations Empty = new([]);

    readonly List<SettingDeclaration> _declarations = [.. declarations];

    /// <inheritdoc/>
    public IReadOnlyList<SettingDeclaration> Of(string system) => [.. _declarations.Where(declaration => declaration.System == system)];
}
