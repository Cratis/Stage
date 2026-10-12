// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Stage.Contracts.Rendering;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

internal static partial class SemanticCratisAdmission
{
    static void ValidateCompliance(SemanticApplicationContext context, List<ArtifactRenderDiagnostic> diagnostics)
    {
        foreach (var concept in context.Application.Concepts.Where(concept => concept.Compliance is not null).Where(concept => !concept.Values.IsEmpty))
        {
            diagnostics.Add(Error(SemanticVersionFeatures.ProtectedIdentity, $"Concept '{concept.Name}' has compliance on an enumeration; its protection cannot be rendered.", concept.Id));
        }

        var protectedUses = IdentityUses(context)
            .Select(identity => (Concept: ProtectedConcept(context, identity.Type), identity.Use))
            .Where(identity => identity.Concept is not null);
        foreach (var (concept, use) in protectedUses)
        {
            diagnostics.Add(Error(SemanticVersionFeatures.ProtectedIdentity, $"Protected concept '{concept!.Name}' is used as {use}. Use a surrogate identity and keep the protected value as a separate property.", concept.Id));
        }
    }

    static SemanticConcept? ProtectedConcept(SemanticApplicationContext context, SemanticTypeReference type)
    {
        if (type.Kind != SemanticTypeReferenceKind.Concept) return null;
        if (!context.Concepts.TryGetValue(type.Target, out var concept)) return null;

        return concept.Compliance is not null ? concept : null;
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

    static IEnumerable<(SemanticTypeReference Type, string Use)> CommandDestinations(IEnumerable<SemanticSlice> slices) =>
        slices.SelectMany(slice => slice.Commands).SelectMany(CommandDestinations);

    static IEnumerable<(SemanticTypeReference Type, string Use)> CommandDestinations(SemanticCommand command) =>
        command.Produces.Select(produced => SemanticDestinations.Of(command, produced)).OfType<SemanticResolvedExpression>()
            .Select(destination => command.Properties.SingleOrDefault(property => property.Id == destination.Target))
            .OfType<SemanticProperty>()
            .Select(property => (property.Type, $"command destination '{command.Name}.{property.Name}'"));

    static IEnumerable<(SemanticTypeReference Type, string Use)> EventSourceIdentities(SemanticApplication application) =>
        application.EventSources.SelectMany(source => TypedIdentity(source.IdentifierType, $"event source identifier '{source.Name}'")
            .Concat(source.Streams.SelectMany(stream => StreamIdentities(source.Name, stream))));

    static IEnumerable<(SemanticTypeReference Type, string Use)> StreamIdentities(string source, SemanticEventStream stream) =>
        TypedIdentity(stream.StreamIdType, $"stream identity '{source}.{stream.Name}'")
            .Concat(stream.StreamIdParts.Select(part => (part.Type, $"stream identity part '{source}.{stream.Name}.{part.Name}'")));

    static IEnumerable<(SemanticTypeReference Type, string Use)> TypedIdentity(SemanticTypeReference? type, string use) =>
        type is null ? [] : [(type, use)];

    static IEnumerable<(SemanticTypeReference Type, string Use)> ProjectionIdentities(SemanticApplicationContext context, IEnumerable<SemanticSlice> slices) =>
        slices.SelectMany(slice => slice.Projections).SelectMany(projection =>
            projection.Transitions.Select(transition => FlatProjectionKeyType(context, transition)).OfType<SemanticTypeReference>()
                .Select(type => (type, $"projection key '{projection.Name}'"))
                .Concat(ScopedProjectionKeyTypes(context, projection.Scope).Select(type => (type, $"projection or removal key '{projection.Name}'"))));

    static SemanticTypeReference? FlatProjectionKeyType(SemanticApplicationContext context, SemanticProjectionTransition transition)
    {
        if (transition.AffectedInstance.Key is not SemanticResolvedExpression key) return null;
        if (!context.Events.TryGetValue(transition.EventContract, out var @event)) return null;

        return @event.Properties.SingleOrDefault(property => property.Id == key.Target)?.Type;
    }

    static IEnumerable<SemanticTypeReference> ScopedProjectionKeyTypes(SemanticApplicationContext context, SemanticProjectionScope? scope) =>
        scope is null ? [] : ProjectionKeys(scope)
            .SelectMany(identity => KeyPaths(identity.Key).Select(path => ProjectionKeyType(context, identity.Contract, path)))
            .OfType<SemanticTypeReference>();

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
