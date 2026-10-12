// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Stage.Contracts.Rendering;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

internal static partial class SemanticCratisAdmission
{
    static void ValidateCompliance(SemanticApplicationContext context, List<ArtifactRenderDiagnostic> diagnostics)
    {
        foreach (var concept in context.Application.Concepts.Where(concept => concept.Compliance is not null && !concept.Values.IsEmpty))
        {
            diagnostics.Add(Error(SemanticVersionFeatures.ProtectedIdentity, $"Concept '{concept.Name}' has compliance on an enumeration; its protection cannot be rendered.", concept.Id));
        }

        foreach (var (type, use) in IdentityUses(context))
        {
            if (type.Kind == SemanticTypeReferenceKind.Concept && context.Concepts.TryGetValue(type.Target, out var concept) && concept.Compliance is not null)
            {
                diagnostics.Add(Error(SemanticVersionFeatures.ProtectedIdentity, $"Protected concept '{concept.Name}' is used as {use}. Use a surrogate identity and keep the protected value as a separate property.", concept.Id));
            }
        }
    }

    static IEnumerable<(SemanticTypeReference Type, string Use)> IdentityUses(SemanticApplicationContext context)
    {
        var slices = SemanticVersionFeatures.Slices(context.Application).ToArray();
        var properties = slices.SelectMany(slice => slice.Commands.SelectMany(command => command.Properties)
            .Concat(slice.ReadModels.SelectMany(model => model.Properties)));
        foreach (var property in properties.Where(property => property.IsIdentifier))
        {
            yield return (property.Type, $"identifier '{property.Name}'");
        }

        foreach (var use in CommandDestinations(slices)) yield return use;
        foreach (var use in EventSourceIdentities(context.Application)) yield return use;
        foreach (var use in ProjectionIdentities(context, slices)) yield return use;
    }

    static IEnumerable<(SemanticTypeReference Type, string Use)> CommandDestinations(IEnumerable<SemanticSlice> slices)
    {
        foreach (var command in slices.SelectMany(slice => slice.Commands))
        {
            foreach (var destination in command.Produces.Select(produced => SemanticDestinations.Of(command, produced)).OfType<SemanticResolvedExpression>())
            {
                var property = command.Properties.SingleOrDefault(property => property.Id == destination.Target);
                if (property is not null) yield return (property.Type, $"command destination '{command.Name}.{property.Name}'");
            }
        }
    }

    static IEnumerable<(SemanticTypeReference Type, string Use)> EventSourceIdentities(SemanticApplication application)
    {
        foreach (var source in application.EventSources)
        {
            if (source.IdentifierType is { } identifier) yield return (identifier, $"event source identifier '{source.Name}'");
            foreach (var stream in source.Streams)
            {
                if (stream.StreamIdType is { } type) yield return (type, $"stream identity '{source.Name}.{stream.Name}'");
                foreach (var part in stream.StreamIdParts) yield return (part.Type, $"stream identity part '{source.Name}.{stream.Name}.{part.Name}'");
            }
        }
    }

    static IEnumerable<(SemanticTypeReference Type, string Use)> ProjectionIdentities(SemanticApplicationContext context, IEnumerable<SemanticSlice> slices)
    {
        foreach (var projection in slices.SelectMany(slice => slice.Projections))
        {
            foreach (var transition in projection.Transitions)
            {
                if (transition.AffectedInstance.Key is SemanticResolvedExpression key && context.Events.TryGetValue(transition.EventContract, out var @event))
                {
                    var property = @event.Properties.SingleOrDefault(property => property.Id == key.Target);
                    if (property is not null) yield return (property.Type, $"projection key '{projection.Name}'");
                }
            }

            if (projection.Scope is not { } scope) continue;
            foreach (var (contract, key) in ProjectionKeys(scope))
            {
                foreach (var path in KeyPaths(key))
                {
                    if (ProjectionKeyType(context, contract, path) is { } type) yield return (type, $"projection or removal key '{projection.Name}'");
                }
            }
        }
    }

    static IEnumerable<(SemanticId Contract, SemanticProjectionKey Key)> ProjectionKeys(SemanticProjectionScope scope) =>
        scope.From.SelectMany(from => Keys(from.EventContract, from.Key, from.ParentKey))
            .Concat(scope.Removals.SelectMany(removal => Keys(removal.EventContract, removal.Key, removal.ParentKey)))
            .Concat(scope.Joins.Where(join => join.Key is not null).Select(join => (join.EventContract, join.Key!)))
            .Concat(scope.JoinRemovals.Select(removal => (removal.EventContract, removal.Key)))
            .Concat(scope.Children.SelectMany(child => ProjectionKeys(child.Scope)))
            .Concat(scope.Nested.SelectMany(nested => ProjectionKeys(nested.Scope)));

    static IEnumerable<(SemanticId Contract, SemanticProjectionKey Key)> Keys(SemanticId contract, SemanticProjectionKey key, SemanticProjectionKey? parent) =>
        parent is null ? [(contract, key)] : [(contract, key), (contract, parent)];

    static IEnumerable<IEnumerable<SemanticId>> KeyPaths(SemanticProjectionKey key) => key switch
    {
        SemanticProjectionValueKey { Value: SemanticProjectionEventProperty property } => [property.Path],
        SemanticProjectionCompositeKey composite => composite.Parts.Select(part => part.Value).OfType<SemanticProjectionEventProperty>().Select(property => property.Path.AsEnumerable()),
        _ => []
    };

    static SemanticTypeReference? ProjectionKeyType(SemanticApplicationContext context, SemanticId contract, IEnumerable<SemanticId> path)
    {
        if (!context.Events.TryGetValue(contract, out var @event)) return null;
        var properties = @event.Properties;
        SemanticProperty? property = null;
        foreach (var id in path)
        {
            property = properties.SingleOrDefault(candidate => candidate.Id == id);
            if (property is null) return null;
            properties = context.Types.TryGetValue(property.Type.Target, out var composite) ? composite.Properties : [];
        }

        return property?.Type;
    }
}
