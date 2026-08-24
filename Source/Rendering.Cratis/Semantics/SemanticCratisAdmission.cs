// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Semantics;
using Cratis.Stage.Contracts.Rendering;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

/// <summary>
/// Admits only the ESM subset the first direct Cratis planner renders exactly.
/// </summary>
internal static class SemanticCratisAdmission
{
    /// <summary>
    /// Evaluates target support for the selected semantic scope.
    /// </summary>
    /// <param name="context">The indexed semantic application.</param>
    /// <param name="slices">The selected slices.</param>
    /// <returns>Blocking diagnostics for unsupported semantics.</returns>
    public static ImmutableArray<ArtifactRenderDiagnostic> Evaluate(
        SemanticApplicationContext context,
        IReadOnlyList<LocatedSemanticSlice> slices)
    {
        var diagnostics = new List<ArtifactRenderDiagnostic>();
        ValidateTypes(context, diagnostics);

        foreach (var located in slices)
        {
            switch (located.Slice.Kind)
            {
                case SemanticSliceKind.StateChange:
                    ValidateStateChange(context, located.Slice, diagnostics);
                    break;
                case SemanticSliceKind.StateView:
                    ValidateStateView(context, located.Slice, diagnostics);
                    break;
                default:
                    diagnostics.Add(Error("STAGE-ESM-001", "The slice kind is not supported by the Cratis ESM planner.", located.Slice.Id));
                    break;
            }

            SemanticSpecificationAdmission.Validate(context, located.Slice, diagnostics);
        }

        return [.. diagnostics];
    }

    static void ValidateTypes(SemanticApplicationContext context, List<ArtifactRenderDiagnostic> diagnostics)
    {
        foreach (var concept in context.Application.Concepts)
        {
            if (concept.Primitive == SemanticPrimitiveType.Unknown ||
                (concept.Values.Length > 0 && concept.Primitive != SemanticPrimitiveType.Text) ||
                concept.Validations.Any(_ => _.Kind != SemanticValidationRuleKind.NotEmpty || _.Operand is not null))
            {
                diagnostics.Add(Error("STAGE-ESM-002", $"Concept '{concept.Name}' uses unsupported values or validation.", concept.Id));
            }
        }

        foreach (var type in context.Application.Types)
        {
            foreach (var property in type.Properties.Where(property => !TypeExists(context, property.Type)))
            {
                diagnostics.Add(Error("STAGE-ESM-003", $"Property '{property.Name}' of '{type.Name}' has an unresolved type.", type.Id));
            }
        }
    }

    static void ValidateStateChange(
        SemanticApplicationContext context,
        SemanticSlice slice,
        List<ArtifactRenderDiagnostic> diagnostics)
    {
        if (slice.Commands.Length != 1)
        {
            diagnostics.Add(Error("STAGE-ESM-004", $"State-change slice '{slice.Name}' must contain exactly one command.", slice.Id));
            return;
        }

        var command = slice.Commands[0];
        if (slice.Events.Any(@event => @event.Revision != EventContractRevision.Initial ||
                @event.Properties.Any(property => !TypeExists(context, property.Type) || property.Type.IsOptional)) ||
            command.Properties.Any(_ => !TypeExists(context, _.Type)) ||
            command.Validations.Any(_ => _.Kind != SemanticValidationRuleKind.NotEmpty || _.Operand is not null) ||
            command.Produces.Length != 1)
        {
            diagnostics.Add(Error("STAGE-ESM-005", $"Command '{command.Name}' exceeds the first Cratis command capability.", command.Id));
            return;
        }

        var produced = command.Produces[0];
        if (!context.Events.TryGetValue(produced.EventContract, out var @event) || produced.Condition is not null ||
            !IsProperty(produced.Destination, SemanticExpressionRootKind.Command, command.Properties.Where(_ => _.IsIdentifier).Select(_ => _.Id)) ||
            @event.Revision != EventContractRevision.Initial || @event.Properties.Any(_ => !TypeExists(context, _.Type) || _.Type.IsOptional) ||
            !MappingsMatch(produced.Mappings, @event.Properties, command.Properties, SemanticExpressionRootKind.Command))
        {
            diagnostics.Add(Error("STAGE-ESM-006", $"Produced event of command '{command.Name}' cannot be rendered without changing its destination or mappings.", command.Id));
        }
    }

    static void ValidateStateView(
        SemanticApplicationContext context,
        SemanticSlice slice,
        List<ArtifactRenderDiagnostic> diagnostics)
    {
        if (slice.ReadModels.Length != 1 || slice.Projections.Length != 1 || slice.Queries.Length > 1)
        {
            diagnostics.Add(Error("STAGE-ESM-007", $"State-view slice '{slice.Name}' exceeds the first Cratis read capability.", slice.Id));
            return;
        }

        var readModel = slice.ReadModels[0];
        var projection = slice.Projections[0];
        if (projection.ReadModel != readModel.Id || projection.Transitions.Length != 1 || readModel.Properties.Any(_ => !TypeExists(context, _.Type)))
        {
            diagnostics.Add(Error("STAGE-ESM-008", $"Projection '{projection.Name}' does not have one resolvable read-model transition.", projection.Id));
            return;
        }

        var transition = projection.Transitions[0];
        if (!context.Events.TryGetValue(transition.EventContract, out var @event) ||
            transition.AffectedInstance.Cardinality != AffectedInstanceCardinality.One ||
            !IsProperty(transition.AffectedInstance.Key, SemanticExpressionRootKind.Event, @event.Properties.Select(_ => _.Id)) ||
            !MappingsMatch(transition.Mappings, readModel.Properties, @event.Properties, SemanticExpressionRootKind.Event) ||
            !UsesEventSourceIdentity(context, transition, @event, readModel))
        {
            diagnostics.Add(Error("STAGE-ESM-009", $"Projection '{projection.Name}' cannot preserve its affected instance with model-bound Cratis projection semantics.", projection.Id));
        }

        if (slice.Queries.SingleOrDefault() is { } query)
        {
            var identifiers = readModel.Properties.Where(_ => _.IsIdentifier).ToArray();
            if (query.ReadModel != readModel.Id || query.Cardinality != SemanticQueryCardinality.ZeroOrOne ||
                query.Delivery != SemanticQueryDelivery.Snapshot || identifiers.Length != 1 ||
                query.KeyProperty != identifiers[0].Id || !TypeExists(context, query.Argument.Type))
            {
                diagnostics.Add(Error("STAGE-ESM-010", $"Query '{query.Name}' is not an optional snapshot lookup by the read-model identifier.", query.Id));
            }
        }
    }

    static bool UsesEventSourceIdentity(
        SemanticApplicationContext context,
        SemanticProjectionTransition transition,
        SemanticEventContract @event,
        SemanticReadModel readModel)
    {
        var eventKey = ((SemanticResolvedExpression)transition.AffectedInstance.Key).Target;
        var eventProperty = @event.Properties.Single(_ => _.Id == eventKey);
        var readModelKeys = readModel.Properties.Where(_ => _.IsIdentifier).ToArray();
        if (readModelKeys.Length != 1 || eventProperty.Type.Kind != SemanticTypeReferenceKind.Concept ||
            !context.IdentifierConcepts.Contains(eventProperty.Type.Target))
        {
            return false;
        }

        var keyMapping = transition.Mappings.SingleOrDefault(_ => _.TargetProperty == readModelKeys[0].Id);
        if (!IsProperty(keyMapping?.Source, SemanticExpressionRootKind.Event, [eventKey]))
        {
            return false;
        }

        var producers = context.Commands.Values.SelectMany(command =>
            command.Produces.Where(produced => produced.EventContract == @event.Id)).ToArray();
        return producers.Length > 0 && producers.All(producer =>
            producer.Destination is SemanticResolvedExpression destination &&
            producer.Mappings.SingleOrDefault(mapping => mapping.TargetProperty == eventKey)?.Source is SemanticResolvedExpression source &&
            destination.Root == SemanticExpressionRootKind.Command && source.Root == SemanticExpressionRootKind.Command &&
            destination.Target == source.Target);
    }

    static bool MappingsMatch(
        ImmutableArray<SemanticPropertyMapping> mappings,
        ImmutableArray<SemanticProperty> targets,
        ImmutableArray<SemanticProperty> sources,
        SemanticExpressionRootKind root) =>
        mappings.Length == targets.Length && targets.All(target => mappings.Any(mapping => mapping.TargetProperty == target.Id &&
            IsProperty(mapping.Source, root, sources.Select(_ => _.Id))));

    static bool IsProperty(SemanticExpression? expression, SemanticExpressionRootKind root, IEnumerable<SemanticId> candidates) =>
        expression is SemanticResolvedExpression { Source: SemanticExpressionSourceKind.Property } resolved &&
        resolved.Root == root && candidates.Contains(resolved.Target);

    static bool TypeExists(SemanticApplicationContext context, SemanticTypeReference type) => type.Kind switch
    {
        SemanticTypeReferenceKind.Primitive => type.Primitive != SemanticPrimitiveType.Unknown,
        SemanticTypeReferenceKind.Concept => context.Concepts.ContainsKey(type.Target),
        SemanticTypeReferenceKind.CompositeType => context.Types.ContainsKey(type.Target),
        _ => false
    };

    static ArtifactRenderDiagnostic Error(string code, string message, SemanticId artifact) =>
        new(code, ArtifactRenderDiagnosticSeverity.Error, message, artifact);
}
