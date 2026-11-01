// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Stage.Contracts.Scene;

/// <summary>
/// Resolves guard fields through read models, query results and string-backed concepts.
/// </summary>
public sealed class SceneGuardFieldTypes
{
    readonly Dictionary<string, PropertySyntax[]> _models;
    readonly Dictionary<string, string> _queries;
    readonly Dictionary<string, string> _concepts;

    /// <summary>Initializes field types from the authored application.</summary>
    /// <param name="syntax">The application declaring the data shapes.</param>
    public SceneGuardFieldTypes(ApplicationSyntax syntax)
    {
        var slices = syntax.Modules.SelectMany(module => Slices(module.Features)).ToArray();
        _models = slices.SelectMany(slice => slice.ReadModels ?? []).GroupBy(model => model.Name, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.First().Properties.ToArray(), StringComparer.Ordinal);
        _queries = slices.SelectMany(slice => slice.Queries ?? []).GroupBy(query => query.Name, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.First().ReturnType.Name, StringComparer.Ordinal);
        _concepts = syntax.Concepts.GroupBy(concept => concept.Name, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.First().Type, StringComparer.Ordinal);
    }

    /// <summary>Whether the subject path is known to hold a string, never a collection or dynamic value.</summary>
    /// <param name="subject">The nearest data declaration's type or query.</param>
    /// <param name="path">The item field path.</param>
    /// <returns>True only for a resolved string field.</returns>
    public bool IsStringField(string? subject, string path)
    {
        if (subject is null || !path.StartsWith(SceneGuards.SubjectPrefix, StringComparison.Ordinal)) return false;
        var model = _queries.GetValueOrDefault(subject, subject);
        foreach (var field in path[SceneGuards.SubjectPrefix.Length..].Split('.'))
        {
            if (!_models.TryGetValue(model, out var properties)) return false;
            var property = properties.FirstOrDefault(property => string.Equals(property.Name, field, StringComparison.OrdinalIgnoreCase));
            if (property?.Type.IsCollection != false) return false;
            model = property.Type.Name;
        }

        return model == "String" || _concepts.GetValueOrDefault(model) == "String";
    }

    static IEnumerable<SliceSyntax> Slices(IEnumerable<FeatureSyntax> features) =>
        features.SelectMany(feature => feature.Slices.Concat(Slices(feature.Features)));
}
