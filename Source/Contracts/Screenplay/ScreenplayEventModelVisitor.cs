// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Stage.Contracts.Screenplay;

/// <summary>
/// Visits a compiled Screenplay <see cref="ApplicationSyntax"/> and produces the Stage <see cref="EventModel"/> the
/// engine runs. Screenplay has no collection concept, so every module is wrapped in a single <see cref="ModuleCollection"/>;
/// identifiers are derived deterministically from names so re-compiling the same source yields the same identity graph.
/// </summary>
public sealed class ScreenplayEventModelVisitor : IApplicationSyntaxVisitor<EventModel>
{
    /// <inheritdoc/>
    public EventModel Visit(ApplicationSyntax syntax)
    {
        var concepts = syntax.Concepts
            .GroupBy(concept => concept.Name, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);

        var schema = new SchemaSynthesizer(concepts);
        var eventPropertyTypes = BuildEventPropertyTypes(syntax.Modules);

        var modules = syntax.Modules.ToArray();
        var modelName = modules.Length > 0 ? modules[0].Name : "EventModel";
        var modelId = DeterministicId.From($"model:{modelName}");
        var collectionId = DeterministicId.From($"model:{modelName}:collection");

        var collectionModules = modules
            .Select(module => ConvertModule(module, modelId, collectionId, schema, eventPropertyTypes))
            .ToArray();

        var collection = new ModuleCollection(collectionId, modelId, collectionModules);

        return new EventModel(modelId, modelName, [collection]);
    }

    static Module ConvertModule(
        ModuleSyntax module,
        Guid modelId,
        Guid collectionId,
        SchemaSynthesizer schema,
        IReadOnlyDictionary<string, string> eventPropertyTypes)
    {
        var features = module.Features
            .Select(feature => ConvertFeature(feature, parentId: null, $"{module.Name}", schema, eventPropertyTypes))
            .ToArray();

        return new Module(DeterministicId.From($"module:{module.Name}"), modelId, collectionId, module.Name, features);
    }

    static Feature ConvertFeature(
        FeatureSyntax feature,
        Guid? parentId,
        string parentPath,
        SchemaSynthesizer schema,
        IReadOnlyDictionary<string, string> eventPropertyTypes)
    {
        var featurePath = $"{parentPath}.{feature.Name}";
        var featureId = DeterministicId.From($"feature:{featurePath}");

        var subFeatures = feature.Features
            .Select(sub => ConvertFeature(sub, featureId, featurePath, schema, eventPropertyTypes))
            .ToArray();

        var slices = feature.Slices
            .Select(slice => SliceConverter.Convert(slice, schema, eventPropertyTypes, featurePath))
            .ToArray();

        return new Feature(featureId, feature.Name, parentId, subFeatures, slices);
    }

    static Dictionary<string, string> BuildEventPropertyTypes(IEnumerable<ModuleSyntax> modules)
    {
        var types = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var slice in modules.SelectMany(module => Slices(module.Features)))
        {
            foreach (var property in slice.Events.SelectMany(@event => @event.Properties))
            {
                types.TryAdd(property.Name, property.Type.Name);
            }
        }

        return types;
    }

    static IEnumerable<SliceSyntax> Slices(IEnumerable<FeatureSyntax> features) =>
        features.SelectMany(feature => feature.Slices.Concat(Slices(feature.Features)));
}
