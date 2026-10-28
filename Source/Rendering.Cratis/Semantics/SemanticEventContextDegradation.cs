// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Semantics;
using Cratis.Stage.Rendering.Cratis.Semantics.Projections;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

/// <summary>
/// Strips projection mappings the renderer cannot express from a read model nothing decides on: $eventContext values (Stage#876)
/// and nested properties reached through an optional property, for which Chronicle's fluent builder has no equivalent.
/// </summary>
internal static class SemanticEventContextDegradation
{
    /// <summary>
    /// Returns the projection and read model without their unsupported mappings, or null when it is left as it is.
    /// </summary>
    /// <param name="projection">The projection.</param>
    /// <param name="context">The semantic application.</param>
    /// <returns>The degraded projection, or null when nothing is stripped or a guard keeps the refusal.</returns>
    internal static DegradedProjection? Strip(SemanticProjection projection, SemanticApplicationContext context)
    {
        if (projection.Scope is not { } scope ||
            !projection.Transitions.IsEmpty ||
            !context.ReadModels.TryGetValue(projection.ReadModel, out var readModel) ||
            !Mappings(scope).Any(mapping => IsUnsupported(mapping, readModel, context)) ||
            Elsewhere(scope) ||
            !Safe(readModel, scope, context))
        {
            return null;
        }

        var unsupported = Mappings(scope).Where(mapping => IsUnsupported(mapping, readModel, context)).ToArray();
        var eventContext = unsupported.Where(IsEventContext).ToArray();
        return new(
            projection with
            {
                Scope = scope with
                {
                    From = [.. scope.From.Select(root => root with { Mappings = Kept(root.Mappings, readModel, context) })],
                    Every = scope.Every is { } every ? every with { Mappings = Kept(every.Mappings, readModel, context) } : null
                }
            },
            readModel with { Properties = [.. readModel.Properties.Select(property => Degrade(property, eventContext))] },
            [
                .. eventContext.Select(mapping => ((SemanticProjectionEventContextValue)mapping.Source!).Path)
                    .Distinct(StringComparer.Ordinal)
                    .Order(StringComparer.Ordinal)
            ],
            [
                .. unsupported.Where(mapping => !IsEventContext(mapping))
                    .Select(mapping => PathName(mapping.Target, readModel.Properties, context))
                    .Distinct(StringComparer.Ordinal)
                    .Order(StringComparer.Ordinal)
            ]);
    }

    static bool IsEventContext(SemanticProjectionMapping mapping) => mapping.Source is SemanticProjectionEventContextValue { Path: not "eventSourceId" };

    // Chronicle's fluent builder cannot set a property under an optional one: the optional object would have to be created from nothing.
    static bool IsThroughOptional(SemanticProjectionMapping mapping, SemanticReadModel readModel, SemanticApplicationContext context)
    {
        if (mapping.Target.Length < 2 || SemanticScopedProjectionSupport.Target(mapping.Target, readModel.Properties, context) is null)
        {
            return false;
        }

        IReadOnlyList<SemanticProperty> properties = readModel.Properties;
        foreach (var id in mapping.Target.SkipLast(1))
        {
            var property = properties.SingleOrDefault(candidate => candidate.Id == id);
            if (property?.Type.IsCollection != false || property.Type.Kind != SemanticTypeReferenceKind.CompositeType || !context.Types.TryGetValue(property.Type.Target, out var composite))
            {
                return false;
            }

            if (property.Type.IsOptional)
            {
                return true;
            }

            properties = composite.Properties;
        }

        return false;
    }

    static bool IsUnsupported(SemanticProjectionMapping mapping, SemanticReadModel readModel, SemanticApplicationContext context) =>
        IsEventContext(mapping) || IsThroughOptional(mapping, readModel, context);

    static ImmutableArray<SemanticProjectionMapping> Kept(ImmutableArray<SemanticProjectionMapping> mappings, SemanticReadModel readModel, SemanticApplicationContext context) =>
        [.. mappings.Where(mapping => !IsUnsupported(mapping, readModel, context))];

    // A required property whose only mapping was stripped is rendered nullable: nothing sets it, so it must not claim a value.
    static SemanticProperty Degrade(SemanticProperty property, SemanticProjectionMapping[] stripped) =>
        !property.Type.IsOptional && !property.Type.IsCollection && stripped.Any(mapping => mapping.Target.Length == 1 && mapping.Target[0] == property.Id)
            ? property with { Type = property.Type with { IsOptional = true } }
            : property;

    static string PathName(ImmutableArray<SemanticId> path, IReadOnlyList<SemanticProperty> properties, SemanticApplicationContext context)
    {
        var names = new List<string>();
        foreach (var id in path)
        {
            var property = properties.Single(candidate => candidate.Id == id);
            names.Add(property.Name);
            properties = SemanticScopedProjectionSupport.Target([id], properties, context) is { Type.Kind: SemanticTypeReferenceKind.CompositeType } composite &&
                context.Types.TryGetValue(composite.Type.Target, out var type) ? type.Properties : [];
        }

        return string.Join('.', names);
    }

    static IEnumerable<SemanticProjectionMapping> Mappings(SemanticProjectionScope scope) =>
        scope.From.SelectMany(root => root.Mappings).Concat(scope.Every?.Mappings ?? []);

    // A join, child or nested scope that carries the value keeps the refusal: they shape identity and membership, not a plain property.
    static bool Elsewhere(SemanticProjectionScope scope) =>
        scope.Joins.Any(join => join.Mappings.Any(IsEventContext)) ||
        scope.Children.Any(child => HasEventContext(child.Scope)) ||
        scope.Nested.Any(nested => HasEventContext(nested.Scope));

    static bool HasEventContext(SemanticProjectionScope scope) =>
        Mappings(scope).Any(IsEventContext) || Elsewhere(scope);

    static bool Safe(SemanticReadModel readModel, SemanticProjectionScope scope, SemanticApplicationContext context)
    {
        var targets = Mappings(scope).Where(mapping => IsUnsupported(mapping, readModel, context)).Select(mapping => mapping.Target).ToArray();
        var properties = targets.Select(target => readModel.Properties.FirstOrDefault(property => target.Length > 0 && property.Id == target[0])).OfType<SemanticProperty>().ToArray();
        if (properties.Length != targets.Length || properties.Any(property => property.IsIdentifier))
        {
            return false;
        }

        var queries = context.Queries.Values.Where(query => query.ReadModel == readModel.Id).ToArray();
        return !ReadByCommand(readModel, context) &&
            !queries.Any(query => UsesProperty(query, properties)) &&
            !queries.Any(query => query.Authorization is { } authorization && SemanticCratisAdmission.OpaquePolicies(authorization, context.Application.Policies).Any());
    }

    static bool ReadByCommand(SemanticReadModel readModel, SemanticApplicationContext context) =>
        context.Commands.Values.Any(command => !command.Reads.IsDefaultOrEmpty && command.Reads.Any(read => read.ReadModel == readModel.Id));

    static bool UsesProperty(SemanticKeyedQuery query, SemanticProperty[] properties) =>
        properties.Any(property => query.KeyProperty == property.Id ||
            string.Equals(query.Argument?.Name, property.Name, StringComparison.OrdinalIgnoreCase) ||
            query.Filters.Any(filter => string.Equals(filter.Name, property.Name, StringComparison.OrdinalIgnoreCase)));
}

/// <summary>
/// A projection and its read model once their unsupported mappings are omitted.
/// </summary>
/// <param name="Projection">The projection without the omitted mappings.</param>
/// <param name="ReadModel">The read model, with properties only an omitted mapping set made nullable.</param>
/// <param name="EventContextPaths">The distinct $eventContext paths omitted.</param>
/// <param name="OptionalPaths">The distinct nested property paths omitted because they sit under an optional property.</param>
internal sealed record DegradedProjection(
    SemanticProjection Projection,
    SemanticReadModel ReadModel,
    ImmutableArray<string> EventContextPaths,
    ImmutableArray<string> OptionalPaths);
