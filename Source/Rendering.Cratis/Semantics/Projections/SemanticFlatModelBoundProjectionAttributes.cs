// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.Naming;

namespace Cratis.Stage.Rendering.Cratis.Semantics.Projections;

/// <summary>
/// Spells the pinned Chronicle attribute API for an admitted flat scope.
/// </summary>
internal static class SemanticFlatModelBoundProjectionAttributes
{
    internal const string Namespace = "global::Cratis.Chronicle.Projections.ModelBound";

    internal static string Identity(SemanticProjectionKey key, SemanticEventContract @event, SemanticProperty property, SemanticApplicationContext context)
    {
        var eventType = new SemanticTypeSystem(context).EventType(@event);
        return key switch
        {
            SemanticProjectionValueKey { Value: SemanticProjectionEventProperty source } => $"{Namespace}.SetFromAttribute<{eventType}>({SemanticFlatModelBoundProjectionValues.Path(source.Path, @event, context)})",
            SemanticProjectionValueKey { Value: SemanticProjectionLiteral literal } => $"{Namespace}.SetValueAttribute<{eventType}>({SemanticFlatModelBoundProjectionValues.Literal(literal, property, context)})",
            _ => $"{Namespace}.SetFromContextAttribute<{eventType}>(\"EventSourceId\")"
        };
    }

    internal static string Mapping(SemanticProjectionMapping mapping, SemanticProjectionFrom from, SemanticProperty property, SemanticApplicationContext context)
    {
        var @event = context.Events[from.EventContract];
        var eventType = new SemanticTypeSystem(context).EventType(@event);
        if (mapping.Operation == SemanticProjectionOperation.Clear) return $"{Namespace}.ClearWithAttribute<{eventType}>";
        if (mapping.Operation is SemanticProjectionOperation.Increment or SemanticProjectionOperation.Decrement) return Counter(mapping, from, eventType);
        if (mapping.Source is SemanticProjectionLiteral literal) return $"{Namespace}.SetValueAttribute<{eventType}>({SemanticFlatModelBoundProjectionValues.Literal(literal, property, context)})";
        var source = Source(mapping, @event, property, context);
        var once = mapping.Operation == SemanticProjectionOperation.SetOnce ? ", SetOnce = true" : string.Empty;
        if (mapping.Source is SemanticProjectionEventContextValue or SemanticProjectionEventSourceIdentity) return $"{Namespace}.SetFromContextAttribute<{eventType}>({source}{once})";
        var operation = mapping.Operation is SemanticProjectionOperation.Set or SemanticProjectionOperation.SetOnce ? "Set" : mapping.Operation.ToString();
        return $"{Namespace}.{operation}FromAttribute<{eventType}>({source}{once})";
    }

    internal static string Join(SemanticProjectionJoin join, SemanticProjectionMapping mapping, SemanticProjection projection, SemanticApplicationContext context)
    {
        var @event = context.Events[join.EventContract];
        var readModel = context.ReadModels[projection.ReadModel];
        var on = readModel.Properties.Single(property => property.Id == join.On);
        var path = ((SemanticProjectionEventProperty)mapping.Source!).Path;
        return $"{Namespace}.JoinAttribute<{new SemanticTypeSystem(context).EventType(@event)}>(on: nameof({Identifiers.ToPascalCase(readModel.Name)}.{Identifiers.ToPascalCase(on.Name)}), eventPropertyName: {SemanticFlatModelBoundProjectionValues.Path(path, @event, context)})";
    }

    internal static IEnumerable<string> Every(SemanticProjectionEvery? every, SemanticProperty property)
    {
        if (every is null) yield break;
        var mappings = every.Mappings.Where(mapping => mapping.Target.SequenceEqual([property.Id])).ToArray();
        if (NeedsAllIdentity(every, property, mappings.Length)) yield return $"{Namespace}.FromAllAttribute(contextProperty: \"EventSourceId\")";
        foreach (var mapping in mappings)
        {
            var source = mapping.Source is SemanticProjectionEventContextValue value ? SemanticScopedProjectionSupport.EventContextProperty(value) : "EventSourceId";
            yield return $"{Namespace}.From{(every.SubscribesToAllEvents ? "All" : "Every")}Attribute(contextProperty: {CSharpCodeBuilder.StringLiteral(source)})";
        }
    }

    static bool NeedsAllIdentity(SemanticProjectionEvery every, SemanticProperty property, int mappingCount) =>
        property.IsIdentifier && every.SubscribesToAllEvents && mappingCount == 0;

    static string Counter(SemanticProjectionMapping mapping, SemanticProjectionFrom from, string eventType) =>
        from.Key is SemanticProjectionValueKey { Value: SemanticProjectionLiteral { Value: SemanticTextValue text } }
            ? $"{Namespace}.{mapping.Operation}Attribute<{eventType}>(ConstantKey = {CSharpCodeBuilder.StringLiteral(text.Value)})"
            : $"{Namespace}.{mapping.Operation}Attribute<{eventType}>";

    static string Source(SemanticProjectionMapping mapping, SemanticEventContract @event, SemanticProperty property, SemanticApplicationContext context)
    {
        if (SemanticProjectionProducts.Of(mapping, @event, [property], context) is { } product) return $"nameof({new SemanticTypeSystem(context).EventType(@event)}.{product.Name})";
        return mapping.Source switch
        {
            SemanticProjectionEventProperty source => SemanticFlatModelBoundProjectionValues.Path(source.Path, @event, context),
            SemanticProjectionEventContextValue value => CSharpCodeBuilder.StringLiteral(SemanticScopedProjectionSupport.EventContextProperty(value)),
            _ => "\"EventSourceId\""
        };
    }
}
