// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Rendering.Cratis.Semantics;

/// <summary>Reviewed reference-bearing ESM members. New identity-bearing members require a classification here.</summary>
internal static class SemanticDependencyReferences
{
    static readonly IReadOnlyDictionary<SemanticDependencyKind, string> _groups = new Dictionary<SemanticDependencyKind, string>
    {
        [SemanticDependencyKind.Declaration] = """
        ExecutableSemanticModel:Application
        SemanticApplication:Concepts Modules Types Policies EventSources Triggers
        SemanticModule:Features
        SemanticFeature:Features Slices
        SemanticSlice:Commands Constraints Events Projections Queries ReadModels Specifications Reducers Reactions Captures
        """,
        [SemanticDependencyKind.TypeReference] = """
        SemanticCompositeType:Properties
        SemanticProperty:Type
        SemanticTypeReference:Target
        SemanticEventContract:Properties
        SemanticReadModel:Properties
        SemanticCommand:Properties
        SemanticReadModelQueryArgument:Type
        SemanticApplicationTrigger:Properties
        """,
        [SemanticDependencyKind.Expression] = """
        SemanticConcept:Validations
        SemanticValidationRule:Property Operand
        SemanticCommand:Validations Produces Destination Requirements Authorization Route Reads Guard Refusals Response Handler
        SemanticCommandHandler:PossibleEvents
        SemanticOneOfExpression:CollectionProperty
        SemanticPropertyMapping:Source TargetProperty
        SemanticStateChangeDestination:Type Value
        SemanticResolvedExpression:Target
        SemanticRefusalExpression:Type
        SemanticEventContextExpression:Type
        SemanticReadPathExpression:PropertyId
        SemanticConditionOperand:Property Value
        SemanticRequirement:Condition
        SemanticInvocationRefusal:Produces
        SemanticComparison:Left Right
        SemanticLogicalCondition:Left Right
        SemanticValueExpression:Value
        SemanticScalarCommandResponse:Source Type
        SemanticRecordCommandResponse:Fields
        SemanticCommandResponseField:Source Type
        """,
        [SemanticDependencyKind.ProducedEvent] = """
        SemanticProducedEvent:EventContract Mappings Destination DestinationType When Condition Route
        """,
        [SemanticDependencyKind.Route] = """
        SemanticCommandRoute:Source Stream StreamId StreamIdParts
        SemanticCommandRoutePart:Value
        SemanticEventSource:IdentifierType Streams
        SemanticEventStream:StreamIdParts StreamIdType
        SemanticStreamIdPart:Type
        """,
        [SemanticDependencyKind.ReadModelRead] = """
        SemanticDecisionRead:ReadModel Key
        SemanticReducer:ReadModel Transitions From
        SemanticReducerTransition:EventContract
        SemanticObserverFilter:Source Stream
        """,
        [SemanticDependencyKind.Projection] = """
        SemanticProjection:ReadModel Transitions Scope
        SemanticProjectionTransition:AffectedInstance EventContract Mappings
        SemanticAffectedInstance:Key
        SemanticProjectionScope:Children From Joins Every JoinRemovals Nested Removals
        SemanticProjectionChildren:IdentifiedBy Property Scope
        SemanticProjectionCompositeKey:Parts Type
        SemanticProjectionPartsKey:Parts
        SemanticProjectionKeyPart:Property Value
        SemanticProjectionEventContextValue:Path
        SemanticProjectionEventProperty:Path
        SemanticProjectionEvery:Mappings
        SemanticProjectionFrom:EventContract Key Mappings ParentKey
        SemanticProjectionJoin:EventContract Mappings On Key
        SemanticProjectionJoinRemoval:EventContract Key
        SemanticProjectionValueKey:Value
        SemanticProjectionMapping:Source Target Factor
        SemanticProjectionLiteral:Value
        SemanticProjectionNested:Property Scope
        SemanticProjectionRemoval:EventContract Key ParentKey
        SemanticChildIdentity:Property
        """,
        [SemanticDependencyKind.Query] = """
        SemanticKeyedQuery:Argument ReadModel KeyProperty Authorization Filters KeyParts
        SemanticQueryKeyPart:Type KeyProperty
        SemanticQueryFilter:Type Property
        SemanticQueryFilterValue:Filter Value
        """,
        [SemanticDependencyKind.Specification] = """
        SemanticSpecification:GivenEvents GivenReadModels ThenEvents ThenReadModels ThenQueries When ThenAbsentReadModels WhenAppended WhenRedelivered GivenCaptures WhenCapture WhenTrigger GivenGenerated ThenReturns ThenInvocations
        SemanticSpecificationAbsentReadModel:Key ReadModel
        SemanticSpecificationRedelivery:Reaction Given
        SemanticSpecificationCommand:Command Values EventSource GeneratedValues
        SemanticSpecificationInvocation:Command Values
        SemanticSpecificationAppend:EventContract EventSource Values Route
        SemanticSpecificationEvent:EventContract EventSource Values Route
        SemanticFixtureRoute:Source Stream StreamId StreamIdParts
        SemanticFixtureRoutePart:Value
        SemanticSpecificationReadModel:Key ReadModel Values
        SemanticSpecificationQueryResult:Key Query Results Filters
        SemanticPropertyValue:TargetProperty Value
        SemanticEventSourceIdentity:Type Value
        SemanticArrayValue:Values
        SemanticCompositeValue:Properties
        SemanticSpecificationCapture:Capture Record
        SemanticCaptureRecord:Fields
        SemanticCaptureField:Value Record Records
        SemanticSpecificationTrigger:Trigger Values
        SemanticSpecificationGeneratedValue:Command Property Value
        SemanticScalarSpecificationResponse:Value
        SemanticRecordSpecificationResponse:Fields
        SemanticSpecificationResponseField:Value
        """,
        [SemanticDependencyKind.Policy] = """
        SemanticPolicyReference:Name
        SemanticLogicalAuthorization:Left Right
        SemanticPolicy:Condition
        SemanticLogicalPolicyCondition:Left Right
        SemanticNotPolicyCondition:Operand
        """,
        [SemanticDependencyKind.Constraint] = """
        SemanticConstraint:Targets ReleasedBy
        SemanticConstraintTarget:EventContract Properties
        """,
        [SemanticDependencyKind.TypedContext] = """
        SemanticTypedContextDescriptor:Members OperationId Types Result
        SemanticHandlerResultDescriptor:Entries
        SemanticHandlerResultEntry:EventContract Payload DestinationType
        SemanticTypedContextMember:Source Type
        SemanticContextSource:SemanticId
        SemanticContextType:ModelType Shape Properties
        SemanticContextTypeDefinition:Id Properties
        SemanticContextProperty:Id Type
        """,
        [SemanticDependencyKind.Reaction] = """
        SemanticReaction:From Triggers
        SemanticReactionTrigger:Source Where Produces Invokes
        SemanticInvocation:Command Mappings OnRefused
        """,
        [SemanticDependencyKind.Capture] = """
        SemanticCapture:EventsSource Appends Children Nested
        SemanticCaptureEventsSource:Events
        SemanticCaptureAppend:EventContract EventSourceType When Mappings
        SemanticCaptureChildren:Appends
        SemanticCaptureNested:Appends
        SemanticCaptureMapping:TargetProperty Value
        """,
        [SemanticDependencyKind.HistoricalEvent] = """
        SemanticEventContract:PriorRevisions
        SemanticEventRevision:Properties
        """
    };

    internal static IReadOnlyDictionary<string, SemanticDependencyClassification> Build()
    {
        var entries = new Dictionary<string, SemanticDependencyClassification>(StringComparer.Ordinal);
        foreach (var group in _groups)
        {
            foreach (var line in group.Value.Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
            {
                var parts = line.Split(':');
                foreach (var member in parts[1].Split(' ')) entries.Add($"{parts[0]}.{member}", new(group.Key));
            }
        }
        return entries;
    }
}
