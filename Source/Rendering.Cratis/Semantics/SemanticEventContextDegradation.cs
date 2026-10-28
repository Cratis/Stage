// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Semantics;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

/// <summary>
/// Strips projection mappings of <c>$eventContext</c> values the renderer cannot express (Stage#876) from a read model nothing decides on.
/// </summary>
internal static class SemanticEventContextDegradation
{
    /// <summary>
    /// Returns the projection without its unsupported event-context mappings, or null when it is left as it is.
    /// </summary>
    /// <param name="projection">The projection.</param>
    /// <param name="context">The semantic application.</param>
    /// <param name="paths">The distinct event-context paths that were stripped.</param>
    /// <returns>The degraded projection, or null when nothing is stripped or a guard keeps the refusal.</returns>
    internal static SemanticProjection? Strip(SemanticProjection projection, SemanticApplicationContext context, out ImmutableArray<string> paths)
    {
        paths = [];
        if (projection.Scope is not { } scope ||
            !projection.Transitions.IsEmpty ||
            !context.ReadModels.TryGetValue(projection.ReadModel, out var readModel) ||
            !RootStrippable(scope) ||
            Elsewhere(scope) ||
            !Safe(readModel, scope, context))
        {
            return null;
        }

        paths =
        [
            .. Mappings(scope).Where(IsUnsupported)
                .Select(mapping => ((SemanticProjectionEventContextValue)mapping.Source).Path)
                .Distinct(StringComparer.Ordinal)
                .Order(StringComparer.Ordinal)
        ];
        return projection with
        {
            Scope = scope with
            {
                From = [.. scope.From.Select(root => root with { Mappings = Kept(root.Mappings) })],
                Every = scope.Every is { } every ? every with { Mappings = Kept(every.Mappings) } : null
            }
        };
    }

    static bool IsUnsupported(SemanticProjectionMapping mapping) => mapping.Source is SemanticProjectionEventContextValue { Path: not "eventSourceId" };

    static ImmutableArray<SemanticProjectionMapping> Kept(ImmutableArray<SemanticProjectionMapping> mappings) => [.. mappings.Where(mapping => !IsUnsupported(mapping))];

    static IEnumerable<SemanticProjectionMapping> Mappings(SemanticProjectionScope scope) =>
        scope.From.SelectMany(root => root.Mappings).Concat(scope.Every?.Mappings ?? []);

    static bool RootStrippable(SemanticProjectionScope scope) => Mappings(scope).Any(IsUnsupported);

    // A join, child or nested scope that carries the value keeps the refusal: they shape identity and membership, not a plain property.
    static bool Elsewhere(SemanticProjectionScope scope) =>
        scope.Joins.Any(join => join.Mappings.Any(IsUnsupported)) ||
        scope.Children.Any(child => HasUnsupported(child.Scope)) ||
        scope.Nested.Any(nested => HasUnsupported(nested.Scope));

    static bool HasUnsupported(SemanticProjectionScope scope) =>
        Mappings(scope).Any(IsUnsupported) || Elsewhere(scope);

    static bool Safe(SemanticReadModel readModel, SemanticProjectionScope scope, SemanticApplicationContext context)
    {
        var targets = Mappings(scope).Where(IsUnsupported).Select(mapping => mapping.Target).ToArray();
        var properties = targets.Select(target => readModel.Properties.FirstOrDefault(property => target.Length > 0 && property.Id == target[0])).OfType<SemanticProperty>().ToArray();
        if (properties.Length != targets.Length || properties.Any(property => property.IsIdentifier))
        {
            return false;
        }

        var queries = context.Queries.Values.Where(query => query.ReadModel == readModel.Id).ToArray();
        return !ReadByCommand(readModel, context) &&
            !queries.Any(query => UsesProperty(query, properties)) &&
            !queries.Any(query => SemanticCratisAdmission.OpaquePolicies(query.Authorization, context.Application.Policies).Any());
    }

    static bool ReadByCommand(SemanticReadModel readModel, SemanticApplicationContext context) =>
        context.Commands.Values.Any(command => !command.Reads.IsDefaultOrEmpty && command.Reads.Any(read => read.ReadModel == readModel.Id));

    static bool UsesProperty(SemanticKeyedQuery query, SemanticProperty[] properties) =>
        properties.Any(property => query.KeyProperty == property.Id ||
            string.Equals(query.Argument?.Name, property.Name, StringComparison.OrdinalIgnoreCase) ||
            query.Filters.Any(filter => string.Equals(filter.Name, property.Name, StringComparison.OrdinalIgnoreCase)));
}
