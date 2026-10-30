// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.Naming;

namespace Cratis.Stage.Rendering.Cratis.Semantics.Projections;

/// <summary>
/// Renders the eligible root scope on its read model without a fluent projection implementation.
/// </summary>
internal static class ModelBoundProjectionRenderer
{
    const string Prefix = "global::Cratis.Chronicle.Projections.ModelBound.";

    internal static void Attributes(CSharpCodeBuilder builder, SemanticProjectionScope scope, SemanticReadModel model, SemanticApplicationContext context)
    {
        if (!AutoMap(scope, model, context))
        {
            builder.Attribute($"{Prefix}NoAutoMapAttribute");
        }

        foreach (var from in scope.From)
        {
            builder.Attribute($"{Prefix}FromEventAttribute<{new SemanticTypeSystem(context).EventType(context.Events[from.EventContract])}>({Key(from.Key, context.Events[from.EventContract], context)})");
        }

        foreach (var removal in scope.Removals)
        {
            builder.Attribute($"{Prefix}RemovedWithAttribute<{new SemanticTypeSystem(context).EventType(context.Events[removal.EventContract])}>({Key(removal.Key, context.Events[removal.EventContract], context)})");
        }
    }

    internal static string Parameters(SemanticProjectionScope scope, SemanticReadModel model, SemanticApplicationContext context, IReadOnlyList<SemanticKeyedQuery> queries)
    {
        var types = new SemanticTypeSystem(context);
        var autoMap = AutoMap(scope, model, context);
        return string.Join(", ", SemanticStateViewArtifactRenderer.OrderedProperties(model.Properties).Select(property =>
        {
            var key = (property.IsIdentifier || queries.Any(query => query.Cardinality == SemanticQueryCardinality.ZeroOrOne && query.KeyProperty == property.Id)) && !types.IsEventSourceIdentifier(property.Type)
                ? "[global::Cratis.Chronicle.Keys.KeyAttribute] " : string.Empty;
            var attributes = scope.From.Select(from => PropertyAttribute(from, property, model, context, autoMap)).Where(attribute => attribute.Length > 0);
            return string.Concat(attributes.Select(attribute => $"[property: {Prefix}{attribute}] ")) + key + $"{types.Type(property.Type)} {Identifiers.ToPascalCase(property.Name)}";
        }));
    }

    internal static bool AutoMap(SemanticProjectionScope scope, SemanticReadModel model, SemanticApplicationContext context) =>
        scope.From.All(from => model.Properties.All(target =>
        {
            var source = context.Events[from.EventContract].Properties.SingleOrDefault(property => SameName(property, target));
            if (source is null)
            {
                return true;
            }

            var mapping = from.Mappings.SingleOrDefault(candidate => candidate.Target[0] == target.Id);
            var sourceType = source.Type with { IsOptional = false };
            var targetType = target.Type with { IsOptional = false };
            return sourceType == targetType && (mapping is null
                ? target.IsIdentifier && from.Key is SemanticProjectionValueKey { Value: SemanticProjectionEventProperty key } && key.Path[0] == source.Id
                : mapping.Operation == SemanticProjectionOperation.Set && mapping.Source is SemanticProjectionEventProperty value && value.Path[0] == source.Id);
        }));

    static string PropertyAttribute(SemanticProjectionFrom from, SemanticProperty property, SemanticReadModel model, SemanticApplicationContext context, bool autoMap)
    {
        var @event = context.Events[from.EventContract];
        var eventType = new SemanticTypeSystem(context).EventType(@event);
        var mapping = from.Mappings.SingleOrDefault(candidate => candidate.Target[0] == property.Id);
        if (mapping is null)
        {
            return property.IsIdentifier ? from.Key switch
            {
                SemanticProjectionValueKey { Value: SemanticProjectionEventProperty key } => SetFrom(key, property, @event, eventType, autoMap),
                SemanticProjectionValueKey { Value: SemanticProjectionEventSourceIdentity } => $"SetFromContextAttribute<{eventType}>(\"EventSourceId\")",
                _ => string.Empty
            } : string.Empty;
        }

        if (SemanticScopedProjectionSupport.IsZeroTotalStart(mapping, property, context))
        {
            return string.Empty;
        }

        var sourceName = mapping.Source is SemanticProjectionEventProperty source ? Name(source, @event) : string.Empty;
        if (SemanticProjectionProducts.Of(mapping, @event, model.Properties, context) is { } product)
        {
            sourceName = product.Name;
        }

        return mapping.Operation switch
        {
            SemanticProjectionOperation.Set when mapping.Source is SemanticProjectionEventProperty setSource => SetFrom(setSource, property, @event, eventType, autoMap),
            SemanticProjectionOperation.Set when mapping.Source is SemanticProjectionEventSourceIdentity => $"SetFromContextAttribute<{eventType}>(\"EventSourceId\")",
            SemanticProjectionOperation.Set when mapping.Source is SemanticProjectionLiteral literal => $"SetValueAttribute<{eventType}>({new SemanticTypeSystem(context).Value(literal.Value, property.Type)})",
            SemanticProjectionOperation.Add => $"AddFromAttribute<{eventType}>(nameof({eventType}.{sourceName}))",
            SemanticProjectionOperation.Subtract => $"SubtractFromAttribute<{eventType}>(nameof({eventType}.{sourceName}))",
            SemanticProjectionOperation.Increment => $"IncrementAttribute<{eventType}>",
            SemanticProjectionOperation.Decrement => $"DecrementAttribute<{eventType}>",
            SemanticProjectionOperation.Clear => $"ClearWithAttribute<{eventType}>",
            _ => throw UnsupportedSemanticRendering.For(nameof(SemanticProjectionOperation), mapping.Operation)
        };
    }

    static string SetFrom(SemanticProjectionEventProperty source, SemanticProperty target, SemanticEventContract @event, string eventType, bool autoMap) =>
        autoMap && Name(source, @event) == Identifiers.ToPascalCase(target.Name)
            ? string.Empty : $"SetFromAttribute<{eventType}>(nameof({eventType}.{Name(source, @event)}))";

    static bool SameName(SemanticProperty left, SemanticProperty right) => Identifiers.ToPascalCase(left.Name) == Identifiers.ToPascalCase(right.Name);

    static string Name(SemanticProjectionEventProperty source, SemanticEventContract @event) =>
        Identifiers.ToPascalCase(@event.Properties.Single(property => property.Id == source.Path[0]).Name);

    static string Key(SemanticProjectionKey key, SemanticEventContract @event, SemanticApplicationContext context) =>
        key is SemanticProjectionValueKey { Value: SemanticProjectionEventProperty source }
            ? $"key: nameof({new SemanticTypeSystem(context).EventType(@event)}.{Name(source, @event)})" : string.Empty;
}
