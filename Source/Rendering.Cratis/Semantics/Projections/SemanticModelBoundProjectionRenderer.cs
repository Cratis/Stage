// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.Naming;

namespace Cratis.Stage.Rendering.Cratis.Semantics.Projections;

/// <summary>
/// Renders flat From-only scoped projections as model-bound read model attributes for applications selecting that convention.
/// Version three expresses sets; version four also expresses running totals (add, subtract, increment, decrement) and clears.
/// </summary>
internal static class SemanticModelBoundProjectionRenderer
{
    const string Namespace = "global::Cratis.Chronicle.Projections.ModelBound";

    /// <summary>
    /// Gets whether the projection is expressed as model-bound attributes. Any other shape stays a fluent projection class.
    /// Under version four, add and subtract by one event property, increment, decrement, clear of an optional property and zero starts are also expressed.
    /// </summary>
    /// <param name="projection">The projection.</param>
    /// <param name="readModel">The projected read model.</param>
    /// <param name="context">The semantic application.</param>
    /// <returns>Whether the projection renders as model-bound attributes.</returns>
    public static bool Expresses(SemanticProjection projection, SemanticReadModel readModel, SemanticApplicationContext context)
    {
        var operations = context.ReducerConvention.ModelBoundOperations;
        if (!context.ReducerConvention.ModelBoundProjections || projection.Scope is not { } scope)
        {
            return false;
        }

        if (!FromOnly(scope) || !ExpressesReadModel(readModel, context))
        {
            return false;
        }

        if (scope.From.Select(from => from.EventContract).Distinct().Count() != scope.From.Length || !scope.From.All(from => ExpressesFrom(from, operations, readModel, context)))
        {
            return false;
        }

        return EveryPropertyHasASource(scope, readModel, operations, context);
    }

    /// <summary>
    /// Gets the attributes that go on the read model type, in scope order.
    /// </summary>
    /// <param name="projection">The projection.</param>
    /// <param name="context">The semantic application.</param>
    /// <returns>The attribute texts without brackets.</returns>
    public static IEnumerable<string> ClassAttributes(SemanticProjection projection, SemanticApplicationContext context)
    {
        var types = new SemanticTypeSystem(context);
        foreach (var from in projection.Scope!.From)
        {
            var @event = context.Events[from.EventContract];
            var name = types.EventType(@event);
            var payloadKey = PayloadKey(from, @event);
            yield return payloadKey is null
                ? $"{Namespace}.FromEventAttribute<{name}>"
                : $"{Namespace}.FromEventAttribute<{name}>(nameof({name}.{payloadKey}))";
        }

        yield return "global::Cratis.Chronicle.Projections.NoAutoMapAttribute";
    }

    /// <summary>
    /// Gets the attribute prefix for one record parameter.
    /// </summary>
    /// <param name="projection">The projection.</param>
    /// <param name="property">The read model property.</param>
    /// <param name="context">The semantic application.</param>
    /// <returns>The bracketed attributes followed by a space, or an empty string.</returns>
    public static string ParameterAttributes(SemanticProjection projection, SemanticProperty property, SemanticApplicationContext context)
    {
        var types = new SemanticTypeSystem(context);
        var attributes = new List<string>();
        foreach (var from in projection.Scope!.From)
        {
            var @event = context.Events[from.EventContract];
            var name = types.EventType(@event);
            var mappings = from.Mappings.Where(mapping => mapping.Target[0] == property.Id).ToArray();
            foreach (var mapping in mappings)
            {
                if (OperationAttribute(mapping, property, name, @event, context) is { } attribute)
                {
                    attributes.Add(attribute);
                }
            }

            if (mappings.Length == 0 && property.IsIdentifier)
            {
                var payloadKey = PayloadKey(from, @event);
                attributes.Add(payloadKey is null
                    ? $"{Namespace}.SetFromContextAttribute<{name}>({CSharpCodeBuilder.StringLiteral("EventSourceId")})"
                    : $"{Namespace}.SetFromAttribute<{name}>(nameof({name}.{payloadKey}))");
            }
        }

        return string.Concat(attributes.Select(attribute => $"[{attribute}] "));
    }

    static bool ExpressesReadModel(SemanticReadModel readModel, SemanticApplicationContext context) =>
        readModel.Properties.Count(property => property.IsIdentifier) == 1 &&
        !SemanticVersionFeatures.HasCompositeKey(readModel) && !context.Reducers.Any(reducer => reducer.ReadModel == readModel.Id);

    // A zero start and a clear render no source attribute, so a property needs another mapping to have a source at all.
    static bool EveryPropertyHasASource(SemanticProjectionScope scope, SemanticReadModel readModel, bool operations, SemanticApplicationContext context)
    {
        var mapped = scope.From.SelectMany(from => from.Mappings)
            .Where(mapping => !operations || HasSource(mapping, readModel, context))
            .Select(mapping => mapping.Target[0]).ToHashSet();
        return readModel.Properties.All(property => property.IsIdentifier || mapped.Contains(property.Id));
    }

    static bool FromOnly(SemanticProjectionScope scope) =>
        scope.Joins.Length == 0 && scope.Children.Length == 0 && scope.Nested.Length == 0 && scope.Removals.Length == 0 &&
        scope.JoinRemovals.Length == 0 && scope.Every is null && scope.From.Length > 0;

    static bool ExpressesFrom(SemanticProjectionFrom from, bool operations, SemanticReadModel readModel, SemanticApplicationContext context) =>
        from.ParentKey is null &&
        from.Key is SemanticProjectionValueKey { Value: SemanticProjectionEventSourceIdentity or SemanticProjectionEventProperty { Path.Length: 1 } } &&
        from.Mappings.All(mapping => ExpressesMapping(mapping, operations, readModel, context));

    static bool ExpressesMapping(SemanticProjectionMapping mapping, bool operations, SemanticReadModel readModel, SemanticApplicationContext context)
    {
        if (mapping.Target.Length != 1 || mapping.Factor is not null)
        {
            return false;
        }

        if (mapping.Operation == SemanticProjectionOperation.Set)
        {
            return mapping.Source is SemanticProjectionEventSourceIdentity or SemanticProjectionEventProperty { Path.Length: 1 };
        }

        return operations && ExpressesOperation(mapping, SemanticScopedProjectionSupport.Target(mapping.Target, readModel.Properties, context), context);
    }

    static bool ExpressesOperation(SemanticProjectionMapping mapping, SemanticProperty? target, SemanticApplicationContext context) =>
        mapping.Operation switch
        {
            SemanticProjectionOperation.Add or SemanticProjectionOperation.Subtract =>
                mapping.Source is SemanticProjectionEventProperty { Path.Length: 1 } || SemanticScopedProjectionSupport.IsZeroTotalStart(mapping, target, context),
            SemanticProjectionOperation.Increment or SemanticProjectionOperation.Decrement => mapping.Source is null,
            SemanticProjectionOperation.Clear => mapping.Source is null && target is { Type.IsOptional: true },
            _ => false
        };

    static bool HasSource(SemanticProjectionMapping mapping, SemanticReadModel readModel, SemanticApplicationContext context) =>
        mapping.Operation != SemanticProjectionOperation.Clear &&
        !SemanticScopedProjectionSupport.IsZeroTotalStart(mapping, SemanticScopedProjectionSupport.Target(mapping.Target, readModel.Properties, context), context);

    static string? OperationAttribute(SemanticProjectionMapping mapping, SemanticProperty property, string name, SemanticEventContract @event, SemanticApplicationContext context)
    {
        if (SemanticScopedProjectionSupport.IsZeroTotalStart(mapping, property, context))
        {
            return null;
        }

        var eventProperty = mapping.Source is SemanticProjectionEventProperty source ? EventProperty(source, @event) : null;
        return mapping.Operation switch
        {
            SemanticProjectionOperation.Add => $"{Namespace}.AddFromAttribute<{name}>(nameof({name}.{eventProperty}))",
            SemanticProjectionOperation.Subtract => $"{Namespace}.SubtractFromAttribute<{name}>(nameof({name}.{eventProperty}))",
            SemanticProjectionOperation.Increment => $"{Namespace}.IncrementAttribute<{name}>",
            SemanticProjectionOperation.Decrement => $"{Namespace}.DecrementAttribute<{name}>",
            SemanticProjectionOperation.Clear => $"{Namespace}.ClearWithAttribute<{name}>",
            _ => eventProperty is not null
                ? $"{Namespace}.SetFromAttribute<{name}>(nameof({name}.{eventProperty}))"
                : $"{Namespace}.SetFromContextAttribute<{name}>({CSharpCodeBuilder.StringLiteral("EventSourceId")})"
        };
    }

    static string? PayloadKey(SemanticProjectionFrom from, SemanticEventContract @event) =>
        from.Key is SemanticProjectionValueKey { Value: SemanticProjectionEventProperty property } ? EventProperty(property, @event) : null;

    static string EventProperty(SemanticProjectionEventProperty property, SemanticEventContract @event) =>
        Identifiers.ToPascalCase(@event.Properties.Single(candidate => candidate.Id == property.Path[0]).Name);
}
