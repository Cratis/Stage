// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Semantics;
using Cratis.Stage.Contracts;

namespace Cratis.Stage.Semantics;

internal sealed class SemanticListCreation
{
    readonly SemanticReadModel[] _models;
    readonly SemanticProjection[] _projections;
    readonly HashSet<SemanticId> _identifierConcepts;
    readonly HashSet<string> _identifierNames;
    readonly HashSet<SemanticId> _candidateEvents;

    internal SemanticListCreation(ExecutableSemanticModel model)
    {
        var slices = SemanticModelWalker.Slices(model).Select(located => located.Slice).ToArray();
        _models = [.. slices.SelectMany(slice => slice.ReadModels)];
        var identifiers = slices.SelectMany(slice => slice.Commands).SelectMany(command => command.Properties)
            .Concat(_models.SelectMany(readModel => readModel.Properties)).Where(property => property.IsIdentifier).ToArray();
        _identifierConcepts =
        [
            .. model.Application.Concepts.Where(concept => concept.Primitive == SemanticPrimitiveType.Uuid).Select(concept => concept.Id),
            .. identifiers.Where(property => property.Type.Kind == SemanticTypeReferenceKind.Concept).Select(property => property.Type.Target)
        ];
        _identifierNames = new(identifiers.Select(property => property.Name), StringComparer.OrdinalIgnoreCase);
        var listed = slices.SelectMany(slice => slice.Queries).Where(query => query.Cardinality == SemanticQueryCardinality.Many).Select(query => query.ReadModel).ToHashSet();
        _projections = [.. slices.SelectMany(slice => slice.Projections).Where(projection => projection.Target == SemanticProjectionTargetKind.ReadModel && listed.Contains(projection.ReadModel))];
        _candidateEvents = [.. _projections.SelectMany(projection => CandidateEvents(projection, _models.Single(readModel => readModel.Id == projection.ReadModel)))];
    }

    internal bool Creates(SemanticCommand command, ConversionDiagnostics diagnostics, string path)
    {
        if (command.Properties.All(property => !property.IsIdentifier || property.IsGenerated)) return true;

        var candidates = command.Produces.Where(production => _candidateEvents.Contains(production.EventContract)).ToArray();
        if (candidates.Length == 0) return false;

        var reason = Refusal(command, candidates);
        if (reason is null) return true;

        diagnostics.Report(new(
            "STAGE-INTENT-001",
            DiagnosticSeverity.Information,
            "command",
            path,
            SourceLocation.Start,
            $"Command '{command.Name}' has no proven list creation intent: {reason}"));
        return false;
    }

    static string? ProjectionRefusal(SemanticProjection projection, SemanticReadModel model, SemanticProperty identifier, SemanticProducedEvent production)
    {
        var keys = model.Properties.Where(property => property.IsIdentifier).ToArray();
        if (keys is not [var key]) return "the listed row must have one key.";
        if (key.Type != identifier.Type) return "the listed row's key must match the command identifier's type.";
        if (!InitializesKey(projection, key.Id, production.EventContract)) return "one unconditional root handler must uniquely initialize the key from $eventSourceId.";
        return AppendsFor(production, identifier.Id) ? null : "the creating event must be appended for the command identifier.";
    }

    static bool AppendsFor(SemanticProducedEvent production, SemanticId identifier) =>
        production.Destination is SemanticResolvedExpression { Root: SemanticExpressionRootKind.Command, Source: SemanticExpressionSourceKind.Property } destination && destination.Target == identifier;

    static IEnumerable<SemanticId> CandidateEvents(SemanticProjection projection, SemanticReadModel model)
    {
        var keys = model.Properties.Where(property => property.IsIdentifier).Select(property => property.Id).ToArray();
        if (keys.Length == 0) return [];
        return projection.Scope is { } scope
            ? ScopedWriters(scope, keys).Select(handler => handler.EventContract)
            : FlatWriters(projection, keys).Select(handler => handler.EventContract);
    }

    static SemanticProjectionTransition[] FlatWriters(SemanticProjection projection, IReadOnlyCollection<SemanticId> keys)
    {
        var writers = projection.Transitions.Where(transition => transition.Mappings.Any(mapping => keys.Contains(mapping.TargetProperty))).ToArray();
        return writers.Length == 0 ? [.. projection.Transitions] : writers;
    }

    static SemanticProjectionFrom[] ScopedWriters(SemanticProjectionScope scope, IReadOnlyCollection<SemanticId> keys)
    {
        var writers = scope.From.Where(handler => handler.Mappings.Any(mapping => mapping.Target.Any(target => keys.Contains(target)))).ToArray();
        return writers.Length == 0 ? [.. scope.From] : writers;
    }

    static bool InitializesKey(SemanticProjection projection, SemanticId key, SemanticId eventContract) =>
        projection.Scope is { } scope ? InitializesScopedKey(scope, key, eventContract) : InitializesFlatKey(projection, key, eventContract);

    static bool InitializesFlatKey(SemanticProjection projection, SemanticId key, SemanticId eventContract) =>
        FlatWriters(projection, [key]) is [var writer] && IsFlatCreator(writer, key, eventContract);

    static bool IsFlatCreator(SemanticProjectionTransition writer, SemanticId key, SemanticId eventContract)
    {
        if (writer.EventContract != eventContract || writer.AffectedInstance.Cardinality == AffectedInstanceCardinality.Many) return false;
        if (writer.AffectedInstance.Key is not SemanticEventContextExpression { Value: SemanticEventContextValueKind.EventSourceIdentity }) return false;
        var assignments = writer.Mappings.Where(mapping => mapping.TargetProperty == key).ToArray();
        return assignments.Length == 0 || assignments is [{ Source: SemanticEventContextExpression { Value: SemanticEventContextValueKind.EventSourceIdentity } }];
    }

    static bool InitializesScopedKey(SemanticProjectionScope scope, SemanticId key, SemanticId eventContract)
    {
        if (!IsRootOnly(scope)) return false;
        return ScopedWriters(scope, [key]) is [var writer] && IsScopedCreator(writer, key, eventContract);
    }

    static bool IsRootOnly(SemanticProjectionScope scope) => scope is
    { Joins.IsEmpty: true, Children.IsEmpty: true, Nested.IsEmpty: true, Every: null, Removals.IsEmpty: true, JoinRemovals.IsEmpty: true };

    static bool IsScopedCreator(SemanticProjectionFrom writer, SemanticId key, SemanticId eventContract)
    {
        if (writer.EventContract != eventContract || writer.ParentKey is not null) return false;
        if (writer.Key is not SemanticProjectionValueKey { Value: SemanticProjectionEventSourceIdentity }) return false;
        var assignments = writer.Mappings.Where(mapping => mapping.Target.Contains(key)).ToArray();
        return assignments.Length == 0 || assignments is [{ Target.Length: 1, Operation: SemanticProjectionOperation.Set, Source: SemanticProjectionEventSourceIdentity }];
    }

    string? Refusal(SemanticCommand command, IEnumerable<SemanticProducedEvent> candidates)
    {
        var identifiers = command.Properties.Where(property => property.IsIdentifier).ToArray();
        if (identifiers is not [var identifier]) return "one command identifier is required.";
        if (command.Route is not null) return "routed commands are not admitted.";
        if (HasSelectionInput(command, identifier)) return "another input may require a selected row or parent key.";

        var reasons = candidates.Select(production => CreationRefusal(identifier, production)).ToArray();
        return reasons.Contains(null) ? null : reasons.FirstOrDefault() ?? "no creating event is produced.";
    }

    string? CreationRefusal(SemanticProperty identifier, SemanticProducedEvent production)
    {
        if (production.Route is not null) return "routed productions are not admitted.";
        var projections = _projections.Where(projection => SemanticProjectionObservation.Observes(projection, production.EventContract)).ToArray();
        if (projections is not [var projection]) return "one listed read-model projection must provide the creation evidence.";
        return ProjectionRefusal(projection, _models.Single(model => model.Id == projection.ReadModel), identifier, production);
    }

    // The runtime's reserved selectedIdentifier alias remains selection-bound even without a modeled identifier declaration.
    bool HasSelectionInput(SemanticCommand command, SemanticProperty identifier) =>
        command.Properties.Where(property => property.Id != identifier.Id).Any(input =>
            input.Type.Primitive == SemanticPrimitiveType.Uuid ||
            _identifierConcepts.Contains(input.Type.Target) ||
            _identifierNames.Contains(input.Name) ||
            input.Name.Equals("selectedIdentifier", StringComparison.OrdinalIgnoreCase));
}
