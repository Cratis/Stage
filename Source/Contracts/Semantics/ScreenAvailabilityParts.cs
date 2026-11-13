// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

namespace Cratis.Stage.Contracts.Semantics;

/// <summary>
/// The exact Arc part keys of gated Scene screens, shared by rendered and live routes.
/// </summary>
public static class ScreenAvailabilityParts
{
    /// <summary>
    /// Maps screen names to marker keys, including screens with inherited module, feature or slice gates.
    /// </summary>
    /// <param name="application">The semantic application.</param>
    /// <returns>The screens that must wait for an availability decision before being shown.</returns>
    public static IReadOnlyDictionary<string, string> For(SemanticApplication application) =>
        application.Modules.SelectMany(module => module.Features.SelectMany(feature => GatedScreens(feature, !module.Available.IsDefaultOrEmpty)))
            .OrderBy(screen => screen.Name, StringComparer.Ordinal)
            .ToDictionary(screen => screen.Name, Key, StringComparer.Ordinal);

    static IEnumerable<SemanticScreen> GatedScreens(SemanticFeature feature, bool inherited)
    {
        var gated = inherited || !feature.Available.IsDefaultOrEmpty;
        return feature.Slices.SelectMany(slice => slice.Screens.Where(screen => gated || !slice.Available.IsDefaultOrEmpty || !screen.Available.IsDefaultOrEmpty))
            .Concat(feature.Features.SelectMany(child => GatedScreens(child, gated)));
    }

    static string Key(SemanticScreen screen) => $"StageScreen_{screen.Id.ToString().Replace(':', '_')}";
}
