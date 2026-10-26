// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.Scene;
using Cratis.Stage.Rendering.Cratis.Semantics;

namespace Cratis.Stage.Rendering.Cratis;

internal static class CratisScaffoldProtocolInputs
{
    static readonly HashSet<string> _names = new(StringComparer.Ordinal)
    {
        StringsCatalogInput.Name,
        AuthoringMetadataInput.Name,
        DomainPlacementInput.Name,
        ScreenPartitionsInput.Name,
        ReducerConventionInput.Name,
        ReducerMigrationInput.Name,
        RefusalModeInput.Name,
        StrictAcceptanceInput.Name,
        FileHeaderInput.Name,
        UserInterfaceInput.Name,
        UserInterfaceDomainsInput.Name
    };

    internal static bool Contains(string name) => _names.Contains(name);
}
