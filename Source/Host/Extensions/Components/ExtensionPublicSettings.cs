// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Host.Extensions.Components;

/// <summary>
/// Gives the browser the settings of a system that are public, and nothing else. A secret never reaches the browser: it is not read, not named and not
/// listed, and a value nobody declared is never sent either.
/// </summary>
/// <param name="values">The <see cref="ISystemSettings"/> that hold the values.</param>
/// <param name="declarations">The <see cref="ISystemSettingDeclarations"/>.</param>
public sealed class ExtensionPublicSettings(ISystemSettings values, ISystemSettingDeclarations declarations)
{
    /// <summary>
    /// Gets the public settings of a system that have a value.
    /// </summary>
    /// <param name="system">The name of the system.</param>
    /// <returns>The values by name.</returns>
    public IReadOnlyDictionary<string, string> Of(string system)
    {
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var declaration in declarations.Of(system).Where(declaration => !declaration.IsSecret))
        {
            if (values.Resolve(system, declaration.Name) is { } value)
            {
                result[declaration.Name] = value;
            }
        }

        return result;
    }
}
