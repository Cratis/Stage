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
    /// <summary>
    /// Refuses described command bodies, atomic reaction batches, protected command reads and composite read-model keys before a legacy path can discard their behavior.
    /// </summary>
    /// <param name="syntax">The application to inspect, including nested features.</param>
    /// <exception cref="InvalidEventModel">Thrown when the application requires behavior the legacy engines cannot preserve.</exception>
    public static void EnsureBehaviorSupported(ApplicationSyntax syntax)
    {
        new ActionSubjectSyntaxAdmission().VisitApplication(syntax);
        new OperationSyntaxAdmission().VisitApplication(syntax);
        if (syntax.ReactionProductionMode is not null)
        {
            throw new InvalidEventModel(
                syntax.Location.Path ?? "<source>",
                ["STAGE-ESM-043: Application declares reaction batches atomic; Stage does not preserve that production boundary yet (Cratis/Stage#422)."]);
        }

        if (syntax.CommandReadMode is not null)
        {
            throw new InvalidEventModel(
                syntax.Location.Path ?? "<source>",
                ["STAGE-ESM-044: Application declares command reads protected; Stage does not preserve protected decision reads yet."]);
        }

        var composite = syntax.Modules.SelectMany(module => Slices(module.Features)).SelectMany(slice => slice.ReadModels ?? [])
            .FirstOrDefault(readModel => readModel.Properties.Count(property => property.IsKey) > 1);
        if (composite is not null)
        {
            throw new InvalidEventModel(
                composite.Location.Path ?? "<source>",
                [$"STAGE-ESM-048: Read model '{composite.Name}' has several key parts (ESM v27 composite read-model keys); the legacy engines key every read model by one identifier and would merge its instances."]);
        }

        foreach (var command in syntax.Modules.SelectMany(module => Slices(module.Features)).SelectMany(slice => slice.Commands))
        {
            var dates = TypedProperties(command, syntax, IsDate);
            var uuids = TypedProperties(command, syntax, IsUuid);
            var blocks = command.Validations.OfType<DeclarativeValidateSyntax>().ToArray();
            var conditions = blocks.SelectMany(block => block.Requirements ?? []).Select(requirement => requirement.Condition)
                .Concat(command.Produces.Where(produced => produced.When is not null).Select(produced => produced.When!)).ToArray();
            var compared = blocks.SelectMany(block => block.Rules).Where(rule =>
                    rule.Rule is ValidationRuleKind.Equal or ValidationRuleKind.NotEqual or ValidationRuleKind.GreaterThan or ValidationRuleKind.GreaterThanOrEqual or ValidationRuleKind.LessThan or ValidationRuleKind.LessThanOrEqual &&
                    rule.Value is LiteralExpressionSyntax).Select(rule => rule.Property)
                .Concat(conditions.SelectMany(condition => ComparedProperties(condition)));
            if (compared.FirstOrDefault(dates.Contains) is { } date)
            {
                throw new InvalidEventModel(
                    command.Location.Path ?? "<source>",
                    [$"STAGE-ESM-046: Command '{command.Name}' compares date property '{date}'; the legacy engines cannot preserve chronological comparisons."]);
            }
            var uuidComparisons = blocks.SelectMany(block => block.Rules)
                .Where(rule => rule.Rule is ValidationRuleKind.Equal or ValidationRuleKind.NotEqual)
                .SelectMany(rule => rule.Value is PathExpressionSyntax path ? new[] { rule.Property, path.Path } : [rule.Property])
                .Concat(conditions.SelectMany(condition => ComparedProperties(condition, includeRight: true)));
            if (compared.Concat(uuidComparisons).FirstOrDefault(uuids.Contains) is { } uuid)
            {
                throw new InvalidEventModel(
                    command.Location.Path ?? "<source>",
                    [$"STAGE-ESM-049: Command '{command.Name}' compares Uuid property '{uuid}' (ESM v28 Uuid equality); the legacy engines cannot preserve canonical Uuid comparisons."]);
            }
            if (command.Handler is { Implementation: not null, File: null, Code: null })
            {
                throw new InvalidEventModel(
                    command.Location.Path ?? "<source>",
                    [$"STAGE-IMPL-002: Command '{command.Name}' has a described command handler; Stage does not implement that body yet."]);
            }
        }
    }

    /// <inheritdoc/>
    public EventModel Visit(ApplicationSyntax syntax) => Convert(syntax, null).Model;

    /// <summary>
    /// Converts an application while retaining diagnostics for omitted projections.
    /// Other unsupported legacy engine behaviors remain subject to their existing admission checks.
    /// </summary>
    /// <param name="syntax">The application to convert.</param>
    /// <returns>The model and conversion diagnostics.</returns>
    public EventModelConversion Convert(ApplicationSyntax syntax) => Convert(syntax, new ConversionDiagnostics());

    static EventModelConversion Convert(ApplicationSyntax syntax, ConversionDiagnostics? diagnostics)
    {
        // The model records protected command reads; the engines that execute commands refuse them at their own boundary.
        EnsureBehaviorSupported(syntax with { CommandReadMode = null });
        var protectedCommandReads = syntax.CommandReadMode is not null;
        new ReactionIdentitySyntaxAdmission().VisitApplication(syntax);
        new EventRouteSyntaxAdmission().VisitApplication(syntax);
        new PublicEventSyntaxAdmission().VisitApplication(syntax);
        new GenerationSyntaxAdmission().VisitApplication(syntax);
        syntax = SpecificationExpansion.Expand(syntax);
        foreach (var slice in syntax.Modules.SelectMany(module => Slices(module.Features)))
        {
            EventGenerationAdmission.EnsureSupported(slice.Events, slice.Name);
        }

        var concepts = syntax.Concepts
            .GroupBy(concept => concept.Name, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);

        var schema = new SchemaSynthesizer(concepts);
        var eventPropertyTypes = BuildEventPropertyTypes(syntax.Modules);
        var eventProperties = BuildEventProperties(syntax.Modules);

        var modules = syntax.Modules.ToArray();
        var modelName = modules.Length > 0 ? modules[0].Name : "EventModel";
        var modelId = DeterministicId.From($"model:{modelName}");
        var collectionId = DeterministicId.From($"model:{modelName}:collection");

        var collectionModules = modules
            .Select(module => ConvertModule(module, modelId, collectionId, schema, eventPropertyTypes, eventProperties, diagnostics))
            .ToArray();

        var collection = new ModuleCollection(collectionId, modelId, collectionModules);

        return new(new EventModel(modelId, modelName, [collection]) { ProtectedCommandReads = protectedCommandReads }, diagnostics?.All ?? []);
    }

    static Module ConvertModule(
        ModuleSyntax module,
        Guid modelId,
        Guid collectionId,
        SchemaSynthesizer schema,
        IReadOnlyDictionary<string, string> eventPropertyTypes,
        IReadOnlyDictionary<string, IReadOnlyList<KeyValuePair<string, string>>> eventProperties,
        ConversionDiagnostics? diagnostics)
    {
        var features = module.Features
            .Select(feature => ConvertFeature(feature, parentId: null, $"{module.Name}", schema, eventPropertyTypes, eventProperties, diagnostics))
            .ToArray();

        return new Module(DeterministicId.From($"module:{module.Name}"), modelId, collectionId, module.Name, features);
    }

    static Feature ConvertFeature(
        FeatureSyntax feature,
        Guid? parentId,
        string parentPath,
        SchemaSynthesizer schema,
        IReadOnlyDictionary<string, string> eventPropertyTypes,
        IReadOnlyDictionary<string, IReadOnlyList<KeyValuePair<string, string>>> eventProperties,
        ConversionDiagnostics? diagnostics)
    {
        var featurePath = $"{parentPath}.{feature.Name}";
        var featureId = DeterministicId.From($"feature:{featurePath}");

        var subFeatures = feature.Features
            .Select(sub => ConvertFeature(sub, featureId, featurePath, schema, eventPropertyTypes, eventProperties, diagnostics))
            .ToArray();

        var slices = feature.Slices
            .Select(slice => diagnostics is null
                ? SliceConverter.Convert(slice, schema, eventPropertyTypes, eventProperties, featurePath)
                : SliceConverter.Convert(slice, schema, eventPropertyTypes, eventProperties, featurePath, diagnostics))
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

    /// <summary>
    /// Collects the properties every declared event carries, keyed by event name.
    /// </summary>
    /// <param name="modules">The modules to collect from.</param>
    /// <returns>The declared properties per event name.</returns>
    /// <remarks>
    /// An <c language="csharp">automap</c> projection states no mappings, so what its read model holds is only knowable from
    /// the events it maps from - which is why the event shapes travel alongside the flat property-type map.
    /// </remarks>
    static Dictionary<string, IReadOnlyList<KeyValuePair<string, string>>> BuildEventProperties(IEnumerable<ModuleSyntax> modules)
    {
        var properties = new Dictionary<string, IReadOnlyList<KeyValuePair<string, string>>>(StringComparer.Ordinal);

        foreach (var slice in modules.SelectMany(module => Slices(module.Features)))
        {
            foreach (var @event in slice.Events)
            {
                properties.TryAdd(@event.Name, [.. @event.Properties.Select(property => new KeyValuePair<string, string>(property.Name, property.Type.Name))]);
            }
        }

        return properties;
    }

    static bool IsDate(string type) => type == "Date" || type == "DateTime";

    static bool IsUuid(string type) => type == "Uuid";

    static HashSet<string> TypedProperties(CommandSyntax command, ApplicationSyntax syntax, Func<string, bool> isType) =>
        command.Properties.Where(property => isType(property.Type.Name) ||
            syntax.Concepts.Any(concept => concept.Name == property.Type.Name && isType(concept.Type))).Select(property => property.Name).ToHashSet(StringComparer.Ordinal);

    static IEnumerable<string> ComparedProperties(ConditionSyntax condition, bool includeRight = false) => condition switch
    {
        ComparisonConditionSyntax comparison => includeRight && comparison.Right is PathExpressionSyntax path ? [comparison.Left, path.Path] : [comparison.Left],
        LogicalConditionSyntax logical => ComparedProperties(logical.Left, includeRight).Concat(ComparedProperties(logical.Right, includeRight)),
        _ => []
    };

    static IEnumerable<SliceSyntax> Slices(IEnumerable<FeatureSyntax> features) =>
        features.SelectMany(feature => feature.Slices.Concat(Slices(feature.Features)));
}
