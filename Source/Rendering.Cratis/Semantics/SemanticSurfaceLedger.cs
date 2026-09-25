// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Rendering.Cratis.Semantics;

/// <summary>
/// Describes how an executable Screenplay semantic member is handled by the Cratis planner.
/// </summary>
internal enum SemanticSurfaceDispositionKind
{
    Rendered,
    Rejected,
    Ignored
}

/// <summary>
/// Records one audited disposition and its diagnostic or rationale.
/// </summary>
/// <param name="Kind">The classification of the member.</param>
/// <param name="Detail">The rejection code or reason for ignoring the member.</param>
internal sealed record SemanticSurfaceDisposition(SemanticSurfaceDispositionKind Kind, string Detail = "");

/// <summary>
/// Inventories the executable semantic surface audited against Screenplay 4.30.0 (ESM v1–v3).
/// A rejected member names the admission diagnostic that blocks its unsupported shape.
/// </summary>
internal static class SemanticSurfaceLedger
{
    /// <summary>
    /// Gets the dispositions keyed by TypeName.MemberName.
    /// </summary>
    public static IReadOnlyDictionary<string, SemanticSurfaceDisposition> Entries { get; } = Build();

    static Dictionary<string, SemanticSurfaceDisposition> Build()
    {
        var entries = new Dictionary<string, SemanticSurfaceDisposition>(StringComparer.Ordinal);
        var rendered = new SemanticSurfaceDisposition(SemanticSurfaceDispositionKind.Rendered);
        SemanticSurfaceDisposition rejected(string code) => new(SemanticSurfaceDispositionKind.Rejected, code);
        SemanticSurfaceDisposition ignored(string reason) => new(SemanticSurfaceDispositionKind.Ignored, reason);

        // Model envelope, identities and application structure. Identifiers locate contracts and scopes;
        // the revision is a semantic input fingerprint, not generated source.
        Add(entries, "ExecutableSemanticModel", rendered, "Application LanguageVersion SemanticVersion");
        Add(entries, "ExecutableSemanticModel", ignored("Canonical input fingerprint; the plan has its own fingerprint."), "Revision");
        Add(entries, "LanguageVersion", rendered, "Major Minor");
        Add(entries, "SemanticVersion", rendered, "Major Minor");
        Add(entries, "SemanticRevision", ignored("Canonical model revision, not an application artifact."), "IsSet");
        Add(entries, "SemanticId", ignored("Identity validity is guaranteed by Screenplay; identity indexes references rather than source members."), "IsSet");
        Add(entries, "EventContractId", ignored("Stable contract identity is already validated by Screenplay; the generated event uses its name."), "IsSet");
        Add(entries, "EventContractRevision", rejected("STAGE-ESM-005"), "IsValid Value");
        Add(entries, "SemanticApplication", rendered, "Concepts Id Modules Name Types Policies");
        Add(entries, "SemanticModule", rendered, "Features Id Name");
        Add(entries, "SemanticFeature", rendered, "Features Id Name Slices");
        Add(entries, "SemanticSlice", rendered, "Commands Constraints Events Id Kind Name Projections Queries ReadModels Specifications");
        Add(entries, "SemanticSlice", rejected("STAGE-ESM-019"), "Reducers");
        Add(entries, "SemanticReducer", rejected("STAGE-ESM-019"), "Name ReadModel Transitions Key InitialState Result");
        Add(entries, "SemanticReducerTransition", rejected("STAGE-ESM-019"), "EventContract RequirementId");
        Add(entries, "SemanticReducerKey", rejected("STAGE-ESM-019"), "EventSourceId");
        Add(entries, "SemanticReducerResult", rejected("STAGE-ESM-019"), "StateOrDelete");
        Add(entries, "SemanticSliceKind", rendered, "StateChange StateView");
        Add(entries, "SemanticSliceKind", rejected("STAGE-ESM-001"), "Unknown");

        // Types, declarations and validation. Properties carry type and identifier semantics into
        // generated records, event destinations, projection keys and specification values.
        Add(entries, "SemanticConcept", rendered, "Id Name Primitive Validations Values");
        Add(entries, "SemanticCompositeType", rendered, "Id Name Properties");
        Add(entries, "SemanticProperty", rendered, "Id IsIdentifier Name Type");
        Add(entries, "SemanticTypeReference", rendered, "IsCollection IsOptional Kind Primitive Target");
        Add(entries, "SemanticTypeReferenceKind", rendered, "Primitive Concept CompositeType");
        Add(entries, "SemanticTypeReferenceKind", rejected("STAGE-ESM-003"), "Unknown");
        Add(entries, "SemanticPrimitiveType", rendered, "Uuid Text WholeNumber DecimalNumber Boolean Date DateTime");
        Add(entries, "SemanticPrimitiveType", rejected("STAGE-ESM-002"), "Unknown");
        Add(entries, "SemanticValidationRule", rendered, "Kind Message Property Severity Operand");
        Add(entries, "SemanticValidationRule", rejected("STAGE-ESM-005"), "Name RequirementId");
        Add(entries, "SemanticValidationRuleKind", rendered, "NotEmpty Maximum Minimum Equal NotEqual GreaterThan GreaterThanOrEqual LessThan LessThanOrEqual Length AllGreaterThan AllGreaterThanOrEqual Matches");
        Add(entries, "SemanticValidationRuleKind", rejected("STAGE-ESM-005"), "Unknown RulePredicate CodeValidation");
        Add(entries, "SemanticValidationSeverity", rendered, "Error");
        Add(entries, "SemanticValidationSeverity", rejected("STAGE-ESM-005"), "Information Warning");

        // State change: one command with unconditional, untagged mapped events in declaration order.
        Add(entries, "SemanticCommand", rendered, "Id Name Properties Validations Produces Destination Requirements Authorization");
        Add(entries, "SemanticCommand", rejected("STAGE-ESM-005"), "CodeValidations");
        Add(entries, "SemanticCodeValidation", rejected("STAGE-ESM-005"), "RequirementId");

        // The compiler exposes attachment metadata separately from the executable model. The planner
        // never receives it; a referenced opaque body is rejected by its owning command, concept or reducer.
        Add(entries, "SemanticImplementationRequirement", ignored("Compiler-only attachment metadata; owning opaque behavior is rejected before rendering."), "Role Owner Member Language File ContentHash Source RequirementId ContextVersion ResultVersion RequiredCapability AttachmentResolution BodySpan BodyLines");
        Add(entries, "SemanticProducedEvent", rendered, "Destination EventContract Mappings");
        Add(entries, "SemanticProducedEvent", rejected("STAGE-ESM-006"), "Condition Tags When");
        Add(entries, "SemanticEventContract", rendered, "Id Name Properties");
        Add(entries, "SemanticEventContract", rejected("STAGE-ESM-006"), "Revision Tags");
        Add(entries, "SemanticEventContract", ignored("The initial event revision's stable contract identity is owned by Screenplay, not emitted by the first renderer."), "ContractId");
        Add(entries, "SemanticPropertyMapping", rendered, "Source TargetProperty");
        Add(entries, "SemanticStateChangeDestination", rendered, "Type Value");
        Add(entries, "SemanticResolvedExpression", rendered, "Root Source Target");
        Add(entries, "SemanticExpression", rendered, "Kind");
        Add(entries, "SemanticExpressionKind", rendered, "Resolved");
        Add(entries, "SemanticExpressionKind", rejected("STAGE-ESM-006"), "Unknown Value EventContext");
        Add(entries, "SemanticExpressionRootKind", rendered, "Command Event");
        Add(entries, "SemanticExpressionRootKind", rejected("STAGE-ESM-006"), "Unknown");
        Add(entries, "SemanticExpressionSourceKind", rendered, "Property");
        Add(entries, "SemanticExpressionSourceKind", rejected("STAGE-ESM-006"), "Unknown");
        Add(entries, "SemanticEventContextExpression", rejected("STAGE-ESM-013"), "Type Value");
        Add(entries, "SemanticEventContextValueKind", rejected("STAGE-ESM-013"), "Unknown EventSourceIdentity Occurred CausedBySubject CausedByName CausedByUserName");
        Add(entries, "SemanticValueExpression", rejected("STAGE-ESM-006"), "Value");

        // State view: one transition keyed by the produced event source and an optional snapshot lookup.
        Add(entries, "SemanticReadModel", rendered, "Id Name Properties");
        Add(entries, "SemanticProjection", rendered, "Id Name ReadModel Transitions");
        Add(entries, "SemanticProjection", rendered, "Scope");
        Add(entries, "SemanticProjectionTransition", rendered, "AffectedInstance EventContract Mappings");
        Add(entries, "SemanticAffectedInstance", rendered, "Cardinality Key");
        Add(entries, "AffectedInstanceCardinality", rendered, "One");
        Add(entries, "AffectedInstanceCardinality", rejected("STAGE-ESM-009"), "Unknown Many ZeroOrOne");
        Add(entries, "SemanticKeyedQuery", rendered, "Argument Cardinality Delivery Id KeyProperty Name ReadModel");
        Add(entries, "SemanticKeyedQuery", rendered, "Authorization");
        Add(entries, "SemanticReadModelQueryArgument", rendered, "Id Name Type");
        Add(entries, "SemanticQueryCardinality", rendered, "ZeroOrOne");
        Add(entries, "SemanticQueryCardinality", rejected("STAGE-ESM-010"), "Unknown One Many");
        Add(entries, "SemanticQueryDelivery", rendered, "Snapshot");
        Add(entries, "SemanticQueryDelivery", rejected("STAGE-ESM-010"), "Unknown Live");

        // Supported scoped blocks render; conflicting roles and nested from without a matching root
        // from and identical key fail STAGE-ESM-017. Composite keys and all-event subscriptions remain
        // rejected: v19.4.7 fluent composite keys differ from the declaration definition, and FromAll
        // omits the all-event subscription. Root join removal is blocked by Chronicle#4125. Nested clear
        // followed by root recreation fails in both ReadModelScenario and the MongoDB sink; child join
        // removal matches the reference in MongoDB but not in ReadModelScenario, so generated specs would fail.
        Add(entries, "SemanticProjectionScope", rendered, "Children From Joins Every JoinRemovals Nested Removals");
        Add(entries, "SemanticProjectionChildren", rendered, "IdentifiedBy Property Scope");
        Add(entries, "SemanticProjectionCompositeKey", rejected("STAGE-ESM-017"), "Parts Type");
        Add(entries, "SemanticProjectionEventContextValue", rejected("STAGE-ESM-017"), "Path");
        Add(entries, "SemanticProjectionEventProperty", rendered, "Path");
        Add(entries, "SemanticProjectionEvery", rendered, "IncludeChildren Mappings");
        Add(entries, "SemanticProjectionEvery", rejected("STAGE-ESM-017"), "SubscribesToAllEvents");
        Add(entries, "SemanticProjectionFrom", rendered, "EventContract Key Mappings ParentKey");
        Add(entries, "SemanticProjectionJoin", rendered, "EventContract Mappings On");
        Add(entries, "SemanticProjectionJoin", rejected("STAGE-ESM-017"), "Key");
        Add(entries, "SemanticProjectionJoinRemoval", rejected("STAGE-ESM-017"), "EventContract Key");
        Add(entries, "SemanticProjectionKey", rendered, "Kind");
        Add(entries, "SemanticProjectionKeyKind", rendered, "Value");
        Add(entries, "SemanticProjectionKeyKind", rejected("STAGE-ESM-017"), "Unknown Composite");
        Add(entries, "SemanticProjectionKeyPart", rejected("STAGE-ESM-017"), "Property Value");
        Add(entries, "SemanticProjectionLiteral", rejected("STAGE-ESM-017"), "Value");
        Add(entries, "SemanticProjectionMapping", rendered, "Operation Source Target");
        Add(entries, "SemanticProjectionNested", rendered, "Property Scope");
        Add(entries, "SemanticProjectionOperation", rendered, "Set Add Subtract Increment Decrement Clear");
        Add(entries, "SemanticProjectionOperation", rejected("STAGE-ESM-017"), "Unknown");
        Add(entries, "SemanticProjectionRemoval", rendered, "EventContract Key ParentKey");
        Add(entries, "SemanticProjectionValue", rendered, "Kind");
        Add(entries, "SemanticProjectionValueKey", rendered, "Value");
        Add(entries, "SemanticProjectionValueKind", rendered, "EventProperty EventSourceIdentity");
        Add(entries, "SemanticProjectionValueKind", rejected("STAGE-ESM-017"), "Unknown EventContext Literal");

        // Specifications render command actions with exact scalar fixtures and supported outcomes.
        // Scoped read-model/query expectations replay every produced event in production order;
        // incomplete or ambiguous unordered duplicate replay fails STAGE-ESM-011. Unordered event
        // assertions compare the produced stream even when the expected fact omits its source.
        // Given events that violate an admitted constraint fail STAGE-ESM-011 before log seeding.
        Add(entries, "SemanticSpecification", rendered, "Id Name GivenEvents GivenReadModels GivenCaller ThenEvents ThenEventsInAnyOrder ThenReadModels ThenQueries ThenErrors ThenDenied When");
        Add(entries, "SemanticSpecification", rejected("STAGE-ESM-011"), "WhenAppended");
        Add(entries, "SemanticSpecificationCommand", rendered, "Command Values EventSource");
        Add(entries, "SemanticSpecificationAppend", rejected("STAGE-ESM-011"), "EventContract EventSource Values");
        Add(entries, "SemanticSpecificationEvent", rendered, "EventContract EventSource Values");
        Add(entries, "SemanticSpecificationReadModel", rendered, "Key ReadModel Values");
        Add(entries, "SemanticSpecificationReadModel", rendered, "Exactly");
        Add(entries, "SemanticSpecificationQueryResult", rendered, "Key Query Results Exactly");
        Add(entries, "SemanticSpecificationError", rendered, "Message");
        Add(entries, "SemanticSpecificationError", rejected("STAGE-ESM-011"), "Code"); // Codes naming a rendered constraint are admitted.
        Add(entries, "SemanticPropertyValue", rendered, "TargetProperty Value");
        Add(entries, "SemanticEventSourceIdentity", rendered, "Type Value");
        Add(entries, "SemanticValue", rendered, "Kind");
        Add(entries, "SemanticTextValue", rendered, "Value");
        Add(entries, "SemanticNumberValue", rendered, "Value");
        Add(entries, "SemanticBooleanValue", rendered, "Value");
        Add(entries, "SemanticArrayValue", rendered, "Values");
        Add(entries, "SemanticCompositeValue", rejected("STAGE-ESM-011"), "Properties");
        Add(entries, "SemanticValueKind", rendered, "Null Text Number Boolean Array");
        Add(entries, "SemanticValueKind", rejected("STAGE-ESM-011"), "Unknown Composite");
        Add(entries, "SemanticNullValue", rendered, "$type");

        // Portable authorization runs through Arc; caller fixtures and command denial are checked by its pipeline.
        // Role claim URIs cannot preserve the separate Screenplay roles/claims boundary (011/015).
        // Command-property requirements render as validator rules.
        Add(entries, "SemanticAuthenticatedCondition", rendered, "$type");
        Add(entries, "SemanticAuthorization", rendered, "$type");
        Add(entries, "SemanticCondition", rendered, "$type");
        Add(entries, "SemanticPolicyCondition", rendered, "$type");
        Add(entries, "SemanticOpaquePolicyCondition", rejected("STAGE-ESM-015"), "RequirementId");
        Add(entries, "SemanticProjectionEventSourceIdentity", rendered, "$type");
        Add(entries, "SemanticCaller", rendered, "Authenticated Claims Roles");
        Add(entries, "SemanticCallerClaim", rendered, "Type Value");
        Add(entries, "SemanticPolicy", rendered, "Condition Name");
        Add(entries, "SemanticPolicyReference", rendered, "Name");
        Add(entries, "SemanticClaimCondition", rendered, "Claim TargetKind Value");
        Add(entries, "SemanticClaimTargetKind", rendered, "Literal Subject Artifact");
        Add(entries, "SemanticRoleCondition", rendered, "Role");
        Add(entries, "SemanticLogicalAuthorization", rendered, "Left Operator Right");
        Add(entries, "SemanticLogicalPolicyCondition", rendered, "Left Operator Right");
        Add(entries, "SemanticRequirement", rendered, "Condition Message Severity");
        Add(entries, "SemanticComparison", rendered, "Left Operator Right");
        Add(entries, "SemanticComparisonOperator", rendered, "Equal NotEqual GreaterThan GreaterThanOrEqual LessThan LessThanOrEqual");
        Add(entries, "SemanticConditionOperand", rendered, "Property Value");
        Add(entries, "SemanticLogicalCondition", rendered, "Left Operator Right");
        Add(entries, "SemanticLogicalOperator", rendered, "And Or");

        // Append-time constraints apply across selected slices even when declared elsewhere. A unique
        // value target requires a non-null command input and rejects intra-command multi-event changes
        // to one constraint (STAGE-ESM-014). Chronicle#4123 maintains indexes after commit: storage
        // failures cannot provide an atomic index-update guarantee, even for admitted constraints.
        Add(entries, "SemanticConstraint", rendered, "IgnoreCasing Kind Message Name ReleasedBy Scope Targets");
        Add(entries, "SemanticConstraintTarget", rendered, "EventContract Properties");
        Add(entries, "SemanticConstraintKind", rendered, "UniquePropertyValue UniqueEventOccurrence");
        Add(entries, "SemanticConstraintKind", rejected("STAGE-ESM-014"), "Unknown");
        Add(entries, "SemanticConstraintScope", rendered, "EventSequence");
        Add(entries, "SemanticConstraintScope", rejected("STAGE-ESM-014"), "Unknown");

        return entries;
    }

    static void Add(
        Dictionary<string, SemanticSurfaceDisposition> entries,
        string type,
        SemanticSurfaceDisposition disposition,
        string members)
    {
        foreach (var member in members.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            entries.Add($"{type}.{member}", disposition);
        }
    }
}
