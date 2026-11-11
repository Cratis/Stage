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
        SemanticApplication:Concepts Modules Types Policies EventSources Triggers Graphs GraphProjections GraphSpecifications GraphQueries
        SemanticGraph:Nodes Edges
        SemanticGraphNode:Properties
        SemanticGraphEdge:Properties
        SemanticModule:Features
        SemanticFeature:Features Slices
        SemanticScreen:Id Data Actions Navigation
        SemanticSlice:Commands Constraints Events Projections Queries ReadModels Specifications Reducers Reactions Captures Screens
        """,
        [SemanticDependencyKind.TypeReference] = """
        SemanticCompositeType:Properties
        SemanticProperty:Type
        SemanticTypeReference:Target
        SemanticEventContract:Properties
        SemanticReadModel:Properties PropertyOrder
        SemanticCommand:Properties
        SemanticReadModelQueryArgument:Type
        SemanticApplicationTrigger:Properties
        SemanticGraphProperty:Type
        """,
        [SemanticDependencyKind.Expression] = """
        SemanticConcept:Validations
        SemanticValidationRule:Property Operand
        SemanticCommand:Validations Produces Destination Requirements Authorization Route Reads Guard Refusals Response Handler
        SemanticPropertyMapping:Source TargetProperty
        SemanticStateChangeDestination:Type Value
        SemanticResolvedExpression:Target
        SemanticRefusalExpression:Type
        SemanticEventContextExpression:Type
        SemanticReadPathExpression:PropertyId
        SemanticOneOfExpression:CollectionProperty
        SemanticConditionOperand:Property Value
        SemanticRequirement:Condition
        SemanticInvocationRefusal:Produces
        SemanticComparison:Left Right
        SemanticLogicalCondition:Left Right
        SemanticValueExpression:Value
        SemanticScalarCommandResponse:Source Type
        SemanticRecordCommandResponse:Fields
        SemanticCommandResponseField:Source Type
        SemanticScreenData:Query
        SemanticScreenAction:Command Inputs Then
        SemanticScreenInput:Property
        SemanticScreenNavigation:Screen
        SemanticProjectionChildren:Ordering
        SemanticProjectionChildrenOrdering:SortBy
        """,
        [SemanticDependencyKind.ProducedEvent] = """
        SemanticProducedEvent:EventContract Mappings Destination DestinationType When Condition Route
        SemanticCommandHandler:PossibleEvents
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
        SemanticGraphProjection:Nodes Edges
        SemanticGraphElementHandlers:Handlers
        SemanticGraphHandler:Event Mappings Key Source Target Identity ForEach
        SemanticGraphMapping:Source Factor
        SemanticProjectionLiteral:Value
        SemanticProjectionNested:Property Scope
        SemanticProjectionRemoval:EventContract Key ParentKey
        SemanticChildIdentity:Property
        """,
        [SemanticDependencyKind.Query] = """
        SemanticKeyedQuery:Argument ReadModel KeyProperty Authorization Filters KeyParts
        SemanticGraphQuery:ReadModel Arguments Conditions Returns Authorization
        SemanticGraphQueryArgument:Type
        SemanticGraphCondition:Literal
        SemanticGraphReturn:Property
        SemanticQueryKeyPart:Type KeyProperty
        SemanticQueryFilter:Type Property
        SemanticQueryFilterValue:Filter Value
        """,
        [SemanticDependencyKind.Specification] = """
        SemanticSpecification:GivenEvents GivenReadModels ThenEvents ThenReadModels ThenQueries When ThenAbsentReadModels WhenAppended WhenRedelivered GivenCaptures WhenCapture WhenTrigger GivenGenerated ThenReturns ThenInvocations
        SemanticSpecificationAbsentReadModel:Key ReadModel
        SemanticGraphSpecification:Given GivenGraphs WhenAppended ThenGraphs ThenQueries
        SemanticGraphQueryExpectation:Arguments Rows
        SemanticGraphRow:Values
        SemanticGraphStep:Elements
        SemanticGraphElementStep:Key Values Target Identity
        SemanticGraphPropertyValue:Value
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
        SemanticPolicyReference:Name Mappings
        SemanticLogicalAuthorization:Left Right
        SemanticPolicy:Condition Reads Inputs
        SemanticStatePolicyCondition:Condition
        SemanticPolicyRead:ReadModel
        SemanticPolicyInput:Type
        SemanticKeyedQuery:Narrowing
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
        SemanticApplication:StateMachines Workflows
        SemanticStateMachine:ReadModel States Transitions Specifications
        SemanticState:Entering
        SemanticStateEntry:Command
        SemanticStateTransition:Event Guard
        SemanticWorkflow:Subject Nodes Edges Specifications
        SemanticWorkflowNode:Event Command Condition Items
        SemanticLifecycleSpecification:Given WhenAppended WhenStarted Then
        SemanticLifecycleExpectation:Command
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
        foreach (var group in _groups) Add(entries, group.Value, new(group.Key));

        // ESM v48 agent allowlists name commands and queries, but Stage renders no agent surface yet (Cratis/Stage#982).
        foreach (var member in new[] { "SemanticApplication.Agents", "SemanticAgents.Operations", "SemanticAgentOperation.Operation" })
        {
            entries.Add(member, new(null, RefusalReason: "agent operation allowlists are not rendered yet (STAGE-ESM-016)."));
        }

        return entries;
    }

    static void Add(Dictionary<string, SemanticDependencyClassification> entries, string members, SemanticDependencyClassification classification)
    {
        foreach (var line in members.Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = line.Split(':');
            foreach (var member in parts[1].Split(' ')) entries.Add($"{parts[0]}.{member}", classification);
        }
    }
}
