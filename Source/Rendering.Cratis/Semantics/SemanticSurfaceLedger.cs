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
/// Inventories the executable semantic surface audited against Screenplay 4.24.0.
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
        Add(entries, "SemanticApplication", rendered, "Concepts Id Modules Name Types");
        Add(entries, "SemanticApplication", rejected("STAGE-ESM-015"), "Policies");
        Add(entries, "SemanticModule", rendered, "Features Id Name");
        Add(entries, "SemanticFeature", rendered, "Features Id Name Slices");
        Add(entries, "SemanticSlice", rendered, "Commands Events Id Kind Name Projections Queries ReadModels Specifications");
        Add(entries, "SemanticSlice", rejected("STAGE-ESM-014"), "Constraints");
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
        Add(entries, "SemanticValidationRule", rendered, "Kind Message Property Severity");
        Add(entries, "SemanticValidationRule", rejected("STAGE-ESM-005"), "Operand");
        Add(entries, "SemanticValidationRuleKind", rendered, "NotEmpty");
        Add(entries, "SemanticValidationRuleKind", rejected("STAGE-ESM-005"), "Unknown Maximum Minimum Equal NotEqual GreaterThan GreaterThanOrEqual LessThan LessThanOrEqual Length AllGreaterThan AllGreaterThanOrEqual Matches");
        Add(entries, "SemanticValidationSeverity", rendered, "Error");
        Add(entries, "SemanticValidationSeverity", rejected("STAGE-ESM-005"), "Information Warning");

        // State change: exactly one simple command and one unconditional, untagged mapped event.
        Add(entries, "SemanticCommand", rendered, "Id Name Properties Validations Produces Destination");
        Add(entries, "SemanticCommand", rejected("STAGE-ESM-005"), "Requirements");
        Add(entries, "SemanticCommand", rejected("STAGE-ESM-015"), "Authorization");
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
        Add(entries, "SemanticProjection", rejected("STAGE-ESM-008"), "Scope");
        Add(entries, "SemanticProjectionTransition", rendered, "AffectedInstance EventContract Mappings");
        Add(entries, "SemanticAffectedInstance", rendered, "Cardinality Key");
        Add(entries, "AffectedInstanceCardinality", rendered, "One");
        Add(entries, "AffectedInstanceCardinality", rejected("STAGE-ESM-009"), "Unknown Many ZeroOrOne");
        Add(entries, "SemanticKeyedQuery", rendered, "Argument Cardinality Delivery Id KeyProperty Name ReadModel");
        Add(entries, "SemanticKeyedQuery", rejected("STAGE-ESM-015"), "Authorization");
        Add(entries, "SemanticReadModelQueryArgument", rendered, "Id Name Type");
        Add(entries, "SemanticQueryCardinality", rendered, "ZeroOrOne");
        Add(entries, "SemanticQueryCardinality", rejected("STAGE-ESM-010"), "Unknown One Many");
        Add(entries, "SemanticQueryDelivery", rendered, "Snapshot");
        Add(entries, "SemanticQueryDelivery", rejected("STAGE-ESM-010"), "Unknown Live");

        // Full declarative projection scopes are not implemented by the first model-bound projection.
        Add(entries, "SemanticProjectionScope", rejected("STAGE-ESM-008"), "Children Every From JoinRemovals Joins Nested Removals");
        Add(entries, "SemanticProjectionChildren", rejected("STAGE-ESM-008"), "IdentifiedBy Property Scope");
        Add(entries, "SemanticProjectionCompositeKey", rejected("STAGE-ESM-008"), "Parts Type");
        Add(entries, "SemanticProjectionEventContextValue", rejected("STAGE-ESM-008"), "Path");
        Add(entries, "SemanticProjectionEventProperty", rejected("STAGE-ESM-008"), "Path");
        Add(entries, "SemanticProjectionEvery", rejected("STAGE-ESM-008"), "IncludeChildren Mappings SubscribesToAllEvents");
        Add(entries, "SemanticProjectionFrom", rejected("STAGE-ESM-008"), "EventContract Key Mappings ParentKey");
        Add(entries, "SemanticProjectionJoin", rejected("STAGE-ESM-008"), "EventContract Key Mappings On");
        Add(entries, "SemanticProjectionJoinRemoval", rejected("STAGE-ESM-008"), "EventContract Key");
        Add(entries, "SemanticProjectionKey", rejected("STAGE-ESM-008"), "Kind");
        Add(entries, "SemanticProjectionKeyKind", rejected("STAGE-ESM-008"), "Unknown Value Composite");
        Add(entries, "SemanticProjectionKeyPart", rejected("STAGE-ESM-008"), "Property Value");
        Add(entries, "SemanticProjectionLiteral", rejected("STAGE-ESM-008"), "Value");
        Add(entries, "SemanticProjectionMapping", rejected("STAGE-ESM-008"), "Operation Source Target");
        Add(entries, "SemanticProjectionNested", rejected("STAGE-ESM-008"), "Property Scope");
        Add(entries, "SemanticProjectionOperation", rejected("STAGE-ESM-008"), "Unknown Set Add Subtract Increment Decrement Clear");
        Add(entries, "SemanticProjectionRemoval", rejected("STAGE-ESM-008"), "EventContract Key ParentKey");
        Add(entries, "SemanticProjectionValue", rejected("STAGE-ESM-008"), "Kind");
        Add(entries, "SemanticProjectionValueKey", rejected("STAGE-ESM-008"), "Value");
        Add(entries, "SemanticProjectionValueKind", rejected("STAGE-ESM-008"), "Unknown EventContext EventProperty EventSourceIdentity Literal");

        // Specifications only render command actions with exact scalar fixtures and supported outcomes.
        Add(entries, "SemanticSpecification", rendered, "Id Name ThenEvents ThenReadModels ThenQueries ThenErrors When");
        Add(entries, "SemanticSpecification", rejected("STAGE-ESM-011"), "GivenCaller GivenEvents GivenReadModels ThenDenied WhenAppended");
        Add(entries, "SemanticSpecification", ignored("At most one expected event is admitted, so event order has no observable effect."), "ThenEventsInAnyOrder");
        Add(entries, "SemanticSpecificationCommand", rendered, "Command Values EventSource");
        Add(entries, "SemanticSpecificationAppend", rejected("STAGE-ESM-011"), "EventContract EventSource Values");
        Add(entries, "SemanticSpecificationEvent", rendered, "EventContract EventSource Values");
        Add(entries, "SemanticSpecificationReadModel", rendered, "Key ReadModel Values");
        Add(entries, "SemanticSpecificationReadModel", ignored("Admission requires all model properties, making subset and exact comparison equivalent."), "Exactly");
        Add(entries, "SemanticSpecificationQueryResult", rendered, "Key Query Results");
        Add(entries, "SemanticSpecificationQueryResult", ignored("A single result for an optional lookup is the only admitted query outcome."), "Exactly");
        Add(entries, "SemanticSpecificationError", ignored("Generated rejection specs assert validation failure, not the error message."), "Message");
        Add(entries, "SemanticSpecificationError", rejected("STAGE-ESM-011"), "Code");
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

        // Caller policies and conditional requirements do not run in generated code.
        Add(entries, "SemanticAuthenticatedCondition", rejected("STAGE-ESM-015"), "$type");
        Add(entries, "SemanticAuthorization", rejected("STAGE-ESM-015"), "$type");
        Add(entries, "SemanticCondition", rejected("STAGE-ESM-005"), "$type");
        Add(entries, "SemanticPolicyCondition", rejected("STAGE-ESM-015"), "$type");
        Add(entries, "SemanticProjectionEventSourceIdentity", rejected("STAGE-ESM-008"), "$type");
        Add(entries, "SemanticCaller", rejected("STAGE-ESM-011"), "Authenticated Claims Roles");
        Add(entries, "SemanticCallerClaim", rejected("STAGE-ESM-011"), "Type Value");
        Add(entries, "SemanticPolicy", rejected("STAGE-ESM-015"), "Condition Name");
        Add(entries, "SemanticPolicyReference", rejected("STAGE-ESM-015"), "Name");
        Add(entries, "SemanticClaimCondition", rejected("STAGE-ESM-015"), "Claim TargetKind Value");
        Add(entries, "SemanticClaimTargetKind", rejected("STAGE-ESM-015"), "Literal Subject Artifact");
        Add(entries, "SemanticRoleCondition", rejected("STAGE-ESM-015"), "Role");
        Add(entries, "SemanticLogicalAuthorization", rejected("STAGE-ESM-015"), "Left Operator Right");
        Add(entries, "SemanticLogicalPolicyCondition", rejected("STAGE-ESM-015"), "Left Operator Right");
        Add(entries, "SemanticRequirement", rejected("STAGE-ESM-005"), "Condition Message Severity");
        Add(entries, "SemanticComparison", rejected("STAGE-ESM-005"), "Left Operator Right");
        Add(entries, "SemanticComparisonOperator", rejected("STAGE-ESM-005"), "Equal NotEqual GreaterThan GreaterThanOrEqual LessThan LessThanOrEqual");
        Add(entries, "SemanticConditionOperand", rejected("STAGE-ESM-005"), "Property Value");
        Add(entries, "SemanticLogicalCondition", rejected("STAGE-ESM-005"), "Left Operator Right");
        Add(entries, "SemanticLogicalOperator", rejected("STAGE-ESM-005"), "And Or");

        // Append-time constraints apply across selected slices even when declared elsewhere.
        Add(entries, "SemanticConstraint", rejected("STAGE-ESM-014"), "IgnoreCasing Kind Message Name ReleasedBy Scope Targets");
        Add(entries, "SemanticConstraintTarget", rejected("STAGE-ESM-014"), "EventContract Properties");
        Add(entries, "SemanticConstraintKind", rejected("STAGE-ESM-014"), "Unknown UniquePropertyValue UniqueEventOccurrence");
        Add(entries, "SemanticConstraintScope", rejected("STAGE-ESM-014"), "Unknown EventSequence");

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
