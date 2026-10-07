// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Runtime;

/// <summary>
/// Translates provider tenant sentinels without conflating named tenants with portable identities.
/// This source is also embedded by the renderer and emitted into generated applications.
/// </summary>
internal static class PortableTenantValues
{
    internal const string Default = "00000000-0000-0000-0000-000000000000";
    internal const string NotSet = "";

    internal static string Translate(string value, string providerDefault, string providerNotSet)
    {
        if (value == providerDefault) return Default;
        if (value == providerNotSet) return NotSet;
        if (value.Length == 0 || (Guid.TryParse(value, out var id) && id == Guid.Empty))
        {
            throw new AmbiguousTenant(value);
        }

        return value;
    }
}
