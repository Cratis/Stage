// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Host.Extensions;

/// <summary>
/// Resolves the settings and secrets of a system. It is the only way a value reaches an extension: never an environment variable or a file Stage writes.
/// </summary>
public interface ISystemSettings
{
    /// <summary>
    /// Resolves one value.
    /// </summary>
    /// <param name="system">The name of the system.</param>
    /// <param name="name">The name of the setting or secret.</param>
    /// <returns>The value, or null when it is not set.</returns>
    string? Resolve(string system, string name);
}

/// <summary>
/// Reads values from configuration under <c lang="text">systems:&lt;System&gt;:&lt;name&gt;</c>, which is <c lang="text">systems__&lt;System&gt;__&lt;name&gt;</c> in the environment of the Stage process
/// and a configuration provider such as a secret store in a deployment.
/// </summary>
/// <param name="configuration">The configuration.</param>
public sealed class ConfigurationSystemSettings(IConfiguration configuration) : ISystemSettings
{
    /// <inheritdoc/>
    public string? Resolve(string system, string name) => configuration[$"systems:{system}:{name}"];
}
