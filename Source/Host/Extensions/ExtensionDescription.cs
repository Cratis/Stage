// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Stage.Extensions.Protocol;

namespace Cratis.Stage.Host.Extensions;

/// <summary>
/// What a running backend says it realizes, as the handshake learned it.
/// </summary>
/// <param name="Name">The extension's name.</param>
/// <param name="Version">The extension's version.</param>
/// <param name="Abilities">The abilities it realizes.</param>
/// <param name="Triggers">The names of the triggers it may raise.</param>
/// <param name="Settings">The settings it reads.</param>
/// <param name="HttpRoutes">The path prefixes it handles over HTTP.</param>
public sealed record ExtensionDescription(
    string Name,
    string Version,
    ImmutableArray<AbilityDeclaration> Abilities,
    ImmutableArray<string> Triggers,
    ImmutableArray<SettingDeclaration> Settings,
    ImmutableArray<string> HttpRoutes)
{
    /// <summary>
    /// Reads the answer of a handshake.
    /// </summary>
    /// <param name="response">The <see cref="DescribeResponse"/>.</param>
    /// <returns>The <see cref="ExtensionDescription"/>.</returns>
    public static ExtensionDescription From(DescribeResponse response) =>
        new(response.Name, response.Version, [.. response.Abilities], [.. response.Triggers.Select(trigger => trigger.Name)], [.. response.Settings], [.. response.HttpRoutes]);
}
