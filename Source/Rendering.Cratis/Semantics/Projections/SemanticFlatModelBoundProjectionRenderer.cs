// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

namespace Cratis.Stage.Rendering.Cratis.Semantics.Projections;

/// <summary>
/// Lowers version-five flat scopes without changing occurrence-local structural contracts.
/// </summary>
internal static class SemanticFlatModelBoundProjectionRenderer
{
    internal static bool Expresses(SemanticProjection projection, SemanticReadModel readModel, SemanticApplicationContext context) =>
        SemanticFlatModelBoundProjectionAdmission.Expresses(projection, readModel, context);

    internal static IEnumerable<string> ClassAttributes(SemanticProjection projection, SemanticApplicationContext context)
    {
        var scope = projection.Scope!;
        foreach (var from in scope.From)
        {
            var @event = context.Events[from.EventContract];
            yield return Attribute("FromEvent", @event, SemanticFlatModelBoundProjectionValues.FromArguments(from, @event, context), context);
        }
        foreach (var removal in scope.Removals)
        {
            var @event = context.Events[removal.EventContract];
            var arguments = SemanticFlatModelBoundProjectionValues.KeyPath(removal.Key, @event, context);
            arguments = SemanticFlatModelBoundProjectionValues.AppendParent(arguments, removal.ParentKey, @event, context);
            yield return Attribute("RemovedWith", @event, arguments, context);
        }
        foreach (var removal in scope.JoinRemovals)
        {
            var @event = context.Events[removal.EventContract];
            yield return Attribute("RemovedWithJoin", @event, SemanticFlatModelBoundProjectionValues.KeyPath(removal.Key, @event, context), context);
        }
        if (scope.Every is { SubscribesToAllEvents: true }) yield return $"{SemanticFlatModelBoundProjectionAttributes.Namespace}.NotRewindableAttribute";
        yield return "global::Cratis.Chronicle.Projections.NoAutoMapAttribute";
    }

    internal static string ParameterAttributes(SemanticProjection projection, SemanticProperty property, SemanticApplicationContext context)
    {
        var attributes = new List<string>();
        var scope = projection.Scope!;
        foreach (var from in scope.From)
        {
            AddFrom(attributes, from, property, context);
        }
        foreach (var join in scope.Joins)
        {
            foreach (var mapping in join.Mappings.Where(mapping => mapping.Target.SequenceEqual([property.Id])))
            {
                attributes.Add(SemanticFlatModelBoundProjectionAttributes.Join(join, mapping, projection, context));
            }
        }
        attributes.AddRange(SemanticFlatModelBoundProjectionAttributes.Every(scope.Every, property));
        return attributes.Count == 0 ? string.Empty : $"[property: {string.Join(", ", attributes)}] ";
    }

    static void AddFrom(List<string> attributes, SemanticProjectionFrom from, SemanticProperty property, SemanticApplicationContext context)
    {
        var mappings = from.Mappings.Where(mapping => mapping.Target.SequenceEqual([property.Id])).ToArray();
        if (property.IsIdentifier && mappings.Length == 0)
        {
            attributes.Add(SemanticFlatModelBoundProjectionAttributes.Identity(from.Key, context.Events[from.EventContract], property, context));
        }
        foreach (var mapping in mappings.Where(mapping => !SemanticScopedProjectionSupport.IsZeroTotalStart(mapping, property, context)))
        {
            attributes.Add(SemanticFlatModelBoundProjectionAttributes.Mapping(mapping, from, property, context));
        }
    }

    static string Attribute(string name, SemanticEventContract @event, string arguments, SemanticApplicationContext context) =>
        $"{SemanticFlatModelBoundProjectionAttributes.Namespace}.{name}Attribute<{new SemanticTypeSystem(context).EventType(@event)}>{(arguments.Length == 0 ? string.Empty : $"({arguments})")}";
}
