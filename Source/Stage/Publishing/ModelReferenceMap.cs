// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Stage.Contracts.Scene;

namespace Cratis.Stage.Publishing;

/// <summary>
/// Pairs the elements of a model with the identifiers its source gave them.
/// </summary>
internal static class ModelReferenceMap
{
    /// <summary>
    /// Lists every qualified element name of the model as <c language="shell">kind:name</c>.
    /// </summary>
    /// <param name="model">The model.</param>
    /// <param name="scene">The scene the model translates to.</param>
    /// <returns>The keys, sorted.</returns>
    internal static IReadOnlyList<string> Keys(ExecutableSemanticModel model, SceneApplication scene)
    {
        var keys = new List<string>();
        foreach (var module in model.Application.Modules)
        {
            keys.Add($"module:{module.Name}");
            foreach (var feature in module.Features)
            {
                Walk(feature, module.Name, keys);
            }
        }

        keys.AddRange(scene.Screens.Select(screen => $"screen:{screen.Name}"));

        return [.. keys.Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal)];
    }

    /// <summary>
    /// Builds the map for the identifiers that were supplied.
    /// </summary>
    /// <param name="keys">The keys of the model.</param>
    /// <param name="supplied">The identifiers the source model supplied.</param>
    /// <returns>The references, sorted; empty when none were supplied.</returns>
    /// <exception cref="StageExportRefused">An identifier names an element the model does not have.</exception>
    internal static IReadOnlyList<ModelReference> Build(IReadOnlyList<string> keys, IReadOnlyDictionary<string, string> supplied)
    {
        var unknown = supplied.Keys.Where(key => !keys.Contains(key, StringComparer.Ordinal)).Order(StringComparer.Ordinal).ToArray();
        if (unknown.Length > 0)
        {
            throw new StageExportRefused([$"The model references name elements the model does not have: {string.Join(", ", unknown)}."]);
        }

        return [.. keys.Where(supplied.ContainsKey).Select(key =>
        {
            var separator = key.IndexOf(':', StringComparison.Ordinal);
            return new ModelReference(key[..separator], key[(separator + 1)..], supplied[key]);
        })];
    }

    static void Walk(SemanticFeature feature, string parent, List<string> keys)
    {
        var name = $"{parent}.{feature.Name}";
        keys.Add($"feature:{name}");
        keys.AddRange(feature.Slices.Select(slice => $"slice:{name}.{slice.Name}"));
        foreach (var nested in feature.Features)
        {
            Walk(nested, name, keys);
        }
    }
}
