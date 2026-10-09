// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Stage.Rendering.Cratis.Semantics.Policies;
using Cratis.Stage.Rendering.Cratis.Semantics.Projections;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

/// <summary>
/// Collects the shared declarations referenced by rendered slices, without changing application-wide naming or placement.
/// </summary>
internal static class SemanticSharedReferences
{
    /// <summary>
    /// Collects concepts and composite types in application declaration order, including transitive dependencies.
    /// </summary>
    /// <param name="context">The indexed application.</param>
    /// <param name="slices">The selected slices.</param>
    /// <returns>The referenced shared declarations.</returns>
    internal static (IReadOnlyList<SemanticConcept> Concepts, IReadOnlyList<SemanticCompositeType> Types) Collect(
        SemanticApplicationContext context,
        IReadOnlyList<LocatedSemanticSlice> slices)
    {
        var concepts = new HashSet<SemanticId>();
        var types = new HashSet<SemanticId>();
        foreach (var located in slices)
        {
            var slice = located.Slice;
            foreach (var @event in slice.Events) Properties(@event.Properties);
            foreach (var command in slice.Commands) Command(command);
            foreach (var readModel in slice.ReadModels) Properties(readModel.Properties);
            foreach (var query in slice.Queries) Query(query);
            foreach (var projection in slice.Projections)
            {
                ReadModel(projection.ReadModel);
                foreach (var eventContract in ProjectionReferencedEventNamesAreUnique.Contracts(projection)) Event(eventContract);
            }
            foreach (var reducer in slice.Reducers)
            {
                ReadModel(reducer.ReadModel);
                foreach (var transition in reducer.Transitions) Event(transition.EventContract);
            }
            foreach (var specification in slice.Specifications)
            {
                foreach (var occurrence in specification.GivenEvents.Concat(specification.ThenEvents))
                {
                    Event(occurrence.EventContract);
                    Reference(occurrence.EventSource?.Type);
                }
                if (specification.When is { } when)
                {
                    Command(context.Commands[when.Command]);
                    Reference(when.EventSource?.Type);
                }
                if (specification.WhenAppended is { } appended)
                {
                    Event(appended.EventContract);
                    Reference(appended.EventSource?.Type);
                }
                foreach (var expected in specification.GivenReadModels.Concat(specification.ThenReadModels)) ReadModel(expected.ReadModel);
                foreach (var absent in specification.ThenAbsentReadModels) ReadModel(absent.ReadModel);
                foreach (var expected in specification.ThenQueries)
                {
                    Query(context.Queries[expected.Query]);
                    foreach (var result in expected.Results) ReadModel(result.ReadModel);
                }
            }
        }

        foreach (var (_, constraint) in SemanticCratisAdmission.SelectedConstraints(context, slices))
        {
            foreach (var target in constraint.Targets) Event(target.EventContract);
            foreach (var released in constraint.ReleasedBy) Event(released);
        }

        var sites = slices.SelectMany(located => located.Slice.Reducers.SelectMany(reducer =>
                reducer.Transitions.Select(transition => (transition.RequirementId, Operation: reducer.ReadModel))))
            .Concat(SemanticPolicyArtifactRenderer.OpaqueSites(context, slices)).ToHashSet();
        foreach (var descriptor in context.Request.TypedContextDescriptors.Where(descriptor =>
            descriptor.OperationId is { } operation && sites.Contains((descriptor.RequirementId, operation))))
        {
            foreach (var definition in descriptor.Types)
            {
                if (definition.Kind == SemanticTypeReferenceKind.Concept) concepts.Add(definition.Id);
                if (definition.Kind == SemanticTypeReferenceKind.CompositeType) Composite(definition.Id);
            }
        }

        // Named sources, stream-id parts and command routes are refused by STAGE-ESM-030 today.
        // Add their type references here when event-source route rendering lands in #177.
        return ([.. context.Application.Concepts.Where(concept => concepts.Contains(concept.Id))],
            [.. context.Application.Types.Where(type => types.Contains(type.Id))]);

        void Properties(IEnumerable<SemanticProperty> properties)
        {
            foreach (var property in properties) Reference(property.Type);
        }

        void Reference(SemanticTypeReference? reference)
        {
            if (reference?.Kind == SemanticTypeReferenceKind.Concept) concepts.Add(reference.Target);
            if (reference?.Kind == SemanticTypeReferenceKind.CompositeType) Composite(reference.Target);
        }

        void Composite(SemanticId id)
        {
            if (types.Add(id)) Properties(context.Types[id].Properties);
        }

        void Event(SemanticId id) => Properties(context.Events[id].Properties);
        void ReadModel(SemanticId id) => Properties(context.ReadModels[id].Properties);

        void Query(SemanticKeyedQuery query)
        {
            Reference(query.Argument?.Type);
            ReadModel(query.ReadModel);
        }

        void Command(SemanticCommand command)
        {
            Properties(command.Properties);
            Reference(command.Destination?.Type);
            foreach (var produced in command.Produces)
            {
                Event(produced.EventContract);
                Reference(produced.DestinationType);
            }
            if (command.Response is SemanticScalarCommandResponse scalar) Reference(scalar.Type);
            if (command.Response is SemanticRecordCommandResponse record)
            {
                foreach (var field in record.Fields) Reference(field.Type);
            }
        }
    }
}
