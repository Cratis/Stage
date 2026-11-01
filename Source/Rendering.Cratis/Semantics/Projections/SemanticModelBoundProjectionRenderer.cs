// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.Naming;

namespace Cratis.Stage.Rendering.Cratis.Semantics.Projections;

/// <summary>
/// Renders flat From-only scoped projections as model-bound read model attributes for applications selecting that convention.
/// </summary>
internal static class SemanticModelBoundProjectionRenderer
{
    const string Namespace = "global::Cratis.Chronicle.Projections.ModelBound";

    /// <summary>
    /// Gets whether the projection is expressed as model-bound attributes. Any other shape stays a fluent projection class.
    /// </summary>
    /// <param name="projection">The projection.</param>
    /// <param name="readModel">The projected read model.</param>
    /// <param name="context">The semantic application.</param>
    /// <returns>Whether the projection renders as model-bound attributes.</returns>
    public static bool Expresses(SemanticProjection projection, SemanticReadModel readModel, SemanticApplicationContext context)
    {
        if (!context.ReducerConvention.ModelBoundProjections || projection.Scope is not { } scope)
        {
            return false;
        }

        if (!FromOnly(scope) || !ExpressesReadModel(readModel, context))
        {
            return false;
        }

        if (scope.From.Select(from => from.EventContract).Distinct().Count() != scope.From.Length || !scope.From.All(ExpressesFrom))
        {
            return false;
        }

        var mapped = scope.From.SelectMany(from => from.Mappings).Select(mapping => mapping.Target[0]).ToHashSet();
        return readModel.Properties.All(property => property.IsIdentifier || mapped.Contains(property.Id));
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
                attributes.Add(mapping.Source is SemanticProjectionEventProperty source
                    ? $"{Namespace}.SetFromAttribute<{name}>(nameof({name}.{EventProperty(source, @event)}))"
                    : $"{Namespace}.SetFromContextAttribute<{name}>({CSharpCodeBuilder.StringLiteral("EventSourceId")})");
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

    static bool FromOnly(SemanticProjectionScope scope) =>
        scope.Joins.Length == 0 && scope.Children.Length == 0 && scope.Nested.Length == 0 && scope.Removals.Length == 0 &&
        scope.JoinRemovals.Length == 0 && scope.Every is null && scope.From.Length > 0;

    static bool ExpressesFrom(SemanticProjectionFrom from) =>
        from.ParentKey is null &&
        from.Key is SemanticProjectionValueKey { Value: SemanticProjectionEventSourceIdentity or SemanticProjectionEventProperty { Path.Length: 1 } } &&
        from.Mappings.All(mapping => mapping.Operation == SemanticProjectionOperation.Set && mapping.Target.Length == 1 && mapping.Factor is null &&
            mapping.Source is SemanticProjectionEventSourceIdentity or SemanticProjectionEventProperty { Path.Length: 1 });

    static string? PayloadKey(SemanticProjectionFrom from, SemanticEventContract @event) =>
        from.Key is SemanticProjectionValueKey { Value: SemanticProjectionEventProperty property } ? EventProperty(property, @event) : null;

    static string EventProperty(SemanticProjectionEventProperty property, SemanticEventContract @event) =>
        Identifiers.ToPascalCase(@event.Properties.Single(candidate => candidate.Id == property.Path[0]).Name);
}
