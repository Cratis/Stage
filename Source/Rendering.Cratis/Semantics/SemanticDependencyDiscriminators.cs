// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

/// <summary>Closed discriminator support for dependency traversal, not rendering admission.</summary>
internal static class SemanticDependencyDiscriminators
{
    const string SupportedValues = """
        AffectedInstanceCardinality:One ZeroOrOne Many
        SemanticQueryCardinality:One ZeroOrOne Many
        SemanticQueryDelivery:Snapshot Live
        SemanticCaptureMapKind:Value Template Split
        SemanticCaptureConditionKind:AnyChanged AllChanged Transition Added Removed Expression
        SemanticCaptureFieldKind:Value Record Records
        SemanticCommandReadMode:Protected
        SemanticConceptFindingTarget:Generation Option Rule
        SemanticComparisonOperator:Equal NotEqual GreaterThan GreaterThanOrEqual LessThan LessThanOrEqual
        SemanticLogicalOperator:And Or
        SemanticConstraintKind:UniquePropertyValue UniqueEventOccurrence
        SemanticConstraintScope:EventSequence
        SemanticDecisionReadPresence:Required
        SemanticDecisionReadRefusal:Reducer Join Children OpenSubscription PayloadKey ByParts NoRemoval EmptyHandledSet NamedSourceOrStream Singleton KeyType GeneratedKey OptionalKey CollectionKey
        SemanticIdentityOrigin:Persisted LegacyBootstrap
        SemanticImplementationRole:CommandHandler CommandValidation ConceptValidation RulePredicate PolicyPredicate ReducerTransition QueryPerformer ConstraintPredicate ReactionEffect ValueGeneration
        SemanticAttachmentResolution:Resolved UnresolvedFile Pending
        SemanticSliceKind:StateChange StateView Automation Translate
        SemanticPerformedQueryArgumentKind:Key Filter
        SemanticClaimTargetKind:Artifact Literal Subject
        SemanticProjectionValueKind:Literal EventProperty EventSourceIdentity EventContext
        SemanticProjectionKeyKind:Value Composite Parts
        SemanticProjectionOperation:Set Clear Add Subtract Increment Decrement
        SemanticEventVisibility:Private Public
        SemanticTranslationDirection:Inbound Outbound
        SemanticProjectionTargetKind:ReadModel Event
        SemanticReactionIdentityKind:System
        SemanticReactionProductionMode:Atomic
        SemanticReactionTriggerKind:Event ApplicationTrigger Startup Shutdown Interval Schedule
        SemanticReducerKey:EventSourceId
        SemanticReducerResult:StateOrDelete
        SemanticRuleStatus:NotEvaluable Satisfied Failed
        SemanticValidationSeverity:Error Information Warning
        SemanticPrimitiveType:Uuid Text WholeNumber DecimalNumber Boolean Date DateTime
        SemanticTypeReferenceKind:Primitive Concept CompositeType
        SemanticValueKind:Null Text Number Boolean Array Composite
        SemanticExpressionKind:Value Resolved EventContext Refusal ReadPath
        SemanticEventContextValueKind:EventSourceIdentity Occurred CausedBySubject CausedByName CausedByUserName
        SemanticExpressionRootKind:Command Event Trigger
        SemanticExpressionSourceKind:Property
        SemanticValidationRuleKind:NotEmpty Maximum Minimum Equal NotEqual GreaterThan GreaterThanOrEqual LessThan LessThanOrEqual Length AllGreaterThan AllGreaterThanOrEqual Matches RulePredicate CodeValidation Checksum Contains NotContains StartsWith EndsWith DecimalPlaces
        """;

    static readonly Dictionary<string, HashSet<string>> _values = SupportedValues.Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
        .Select(line => line.Split(':')).ToDictionary(parts => parts[0], parts => new HashSet<string>(parts[1].Split(' '), StringComparer.Ordinal), StringComparer.Ordinal);

    internal static bool Supports(Enum value)
    {
        if (value.GetType().Namespace != typeof(SemanticApplication).Namespace) return false;
        return _values.TryGetValue(value.GetType().Name, out var names) && names.Contains(value.ToString());
    }
}
