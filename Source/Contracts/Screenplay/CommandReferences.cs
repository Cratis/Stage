// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Stage.Contracts.Screenplay;

/// <summary>
/// Resolves identifier concepts to an unambiguous modeled list query, without planning its runtime route.
/// </summary>
internal sealed class CommandReferences
{
    static readonly string[] LabelNames = ["name", "title", "displayName"];

    readonly SemanticApplication _application;
    readonly SemanticReadModel[] _models;
    readonly SemanticKeyedQuery[] _queries;
    readonly HashSet<SemanticId> _identifiers;
    readonly ConversionDiagnostics? _diagnostics;
    readonly string _path;
    readonly SourceLocation _location;

    internal CommandReferences(SemanticApplication application, ConversionDiagnostics? diagnostics, string path, SourceLocation location)
    {
        _application = application;
        _diagnostics = diagnostics;
        _path = path;
        _location = location;
        var slices = application.Modules.SelectMany(module => Slices(module.Features)).ToArray();
        _models = [.. slices.SelectMany(slice => slice.ReadModels)];
        _queries = [.. slices.SelectMany(slice => slice.Queries)];
        _identifiers = [.. slices.SelectMany(slice => slice.Commands.SelectMany(command => command.Properties))
            .Concat(_models.SelectMany(model => model.Properties).Where(property => property.IsIdentifier || IsQueryKey(property)))
            .Where(property => property.Type.Kind == SemanticTypeReferenceKind.Concept && (property.IsIdentifier || IsQueryKey(property)))
            .Select(property => property.Type.Target)];
    }

    internal JsonObject? For(PropertySyntax property)
    {
        if (property.IsIdentifier || property.Type.IsCollection) return null;
        var concept = _application.Concepts.SingleOrDefault(concept => concept.Name == property.Type.Name);
        if (concept is null || !_identifiers.Contains(concept.Id)) return null;

        var models = ModelsFor(concept);
        var pairs = models.SelectMany(model => _queries.Where(query => query.ReadModel == model.Id && CanList(query)).Select(query => (Model: model, Query: query))).ToArray();
        if (pairs.Length != 1)
        {
            Report(property.Name, concept, models, pairs.Select(pair => pair.Query));
            return null;
        }

        var pair = pairs[0];
        var key = Keys(pair.Model, concept.Id).Single();
        return new JsonObject { ["query"] = pair.Query.Name, ["value"] = MemberName(key.Name), ["label"] = MemberName(Label(pair.Model, key).Name) };
    }

    static bool CanList(SemanticKeyedQuery query) => query.Cardinality == SemanticQueryCardinality.Many
        && query.Argument?.Type.IsOptional != false
        && query.KeyParts.IsDefaultOrEmpty
        && query.Filters.All(filter => filter.Type.IsOptional);

    static string Names(IEnumerable<string> names) => string.Join(", ", names.DefaultIfEmpty("none"));

    static string MemberName(string name) => char.ToLowerInvariant(name[0]) + name[1..];

    static IEnumerable<SemanticSlice> Slices(IEnumerable<SemanticFeature> features) => features.SelectMany(feature => feature.Slices.Concat(Slices(feature.Features)));

    SemanticReadModel[] ModelsFor(SemanticConcept concept)
    {
        var models = _models.Where(model => Keys(model, concept.Id).Length == 1).ToArray();
        var name = concept.Name.EndsWith("Id", StringComparison.Ordinal) ? concept.Name[..^2] : concept.Name;
        var preferred = models.Where(model => string.Equals(model.Name, name, StringComparison.OrdinalIgnoreCase)).ToArray();
        return preferred.Length > 0 ? preferred : models;
    }

    SemanticProperty[] Keys(SemanticReadModel model, SemanticId concept) =>
        [.. model.Properties.Where(property => property.Type.Kind == SemanticTypeReferenceKind.Concept && property.Type.Target == concept && !property.Type.IsCollection && (property.IsIdentifier || IsQueryKey(property)))];

    bool IsQueryKey(SemanticProperty property) => _queries.Any(query => query.KeyProperty == property.Id);

    SemanticProperty Label(SemanticReadModel model, SemanticProperty key)
    {
        var text = model.Properties.Where(property => !property.Type.IsCollection && IsText(property.Type)).ToArray();
        return LabelNames.Select(name => text.FirstOrDefault(property => string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase)))
            .FirstOrDefault(property => property is not null) ?? text.FirstOrDefault(property => property.Id != key.Id) ?? key;
    }

    bool IsText(SemanticTypeReference type) => type.Kind == SemanticTypeReferenceKind.Primitive
        ? type.Primitive == SemanticPrimitiveType.Text
        : type.Kind == SemanticTypeReferenceKind.Concept && _application.Concepts.Any(concept => concept.Id == type.Target && concept.Primitive == SemanticPrimitiveType.Text);

    void Report(string property, SemanticConcept concept, SemanticReadModel[] models, IEnumerable<SemanticKeyedQuery> candidates)
    {
        var queries = candidates.Select(query => query.Name).ToArray();
        var seen = _queries.Where(query => models.Any(model => model.Id == query.ReadModel)).Select(query => query.Name);
        _diagnostics?.Report(new(
            "STAGE-SCENE-REFERENCE-001",
            DiagnosticSeverity.Information,
            "reference picker",
            $"{_path}.{property}",
            _location,
            $"Identifier concept '{concept.Name}' has {queries.Length} eligible reference pairs. Read models: {Names(models.Select(model => model.Name))}; queries: {Names(seen)}. Keeping the text field."));
    }
}
