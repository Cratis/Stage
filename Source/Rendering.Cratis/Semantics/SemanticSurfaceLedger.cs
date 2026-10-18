// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

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
/// Inventories the executable semantic surface audited against Screenplay 4.206.0; exact ESM v1–v28 pairs are admitted, but evolved events require migration rendering
/// and each v5–v10 construct Stage does not render yet refuses the model with its own diagnostic.
/// A rejected member names the admission diagnostic that blocks its unsupported shape.
/// </summary>
internal static class SemanticSurfaceLedger
{
    /// <summary>
    /// Gets the dispositions keyed by TypeName.MemberName.
    /// </summary>
    public static IReadOnlyDictionary<string, SemanticSurfaceDisposition> Entries { get; } = Build();

    /// <summary>
    /// Gets the query shape matrix. Snapshot optional lookups require a key; snapshot and live collections may be keyed or unkeyed.
    /// Other cardinality/delivery/key combinations fail STAGE-ESM-010 before rendering.
    /// </summary>
    public static IReadOnlyDictionary<(SemanticQueryCardinality Cardinality, SemanticQueryDelivery Delivery, bool Keyed), SemanticSurfaceDisposition> QueryShapes { get; } =
        (from cardinality in Enum.GetValues<SemanticQueryCardinality>()
         from delivery in Enum.GetValues<SemanticQueryDelivery>()
         from keyed in new[] { false, true }
         select (Cardinality: cardinality, Delivery: delivery, Keyed: keyed))
        .ToDictionary(shape => shape, shape =>
            (shape.Cardinality == SemanticQueryCardinality.ZeroOrOne && shape.Delivery == SemanticQueryDelivery.Snapshot && shape.Keyed) ||
            (shape.Cardinality == SemanticQueryCardinality.Many && shape.Delivery is SemanticQueryDelivery.Snapshot or SemanticQueryDelivery.Live)
                ? new SemanticSurfaceDisposition(SemanticSurfaceDispositionKind.Rendered)
                : new SemanticSurfaceDisposition(SemanticSurfaceDispositionKind.Rejected, "STAGE-ESM-010"));

    /// <summary>
    /// Gets reaction side-effect combinations. Events and commands from one trigger cannot share an Arc handler result
    /// at the pinned Arc version; event-only, command-only and empty results render.
    /// </summary>
    public static IReadOnlyDictionary<(bool Produces, bool Invokes), SemanticSurfaceDisposition> ReactionSideEffectShapes { get; } =
        (from produces in new[] { false, true }
         from invokes in new[] { false, true }
         select (Produces: produces, Invokes: invokes))
        .ToDictionary(shape => shape, shape => shape.Produces && shape.Invokes
            ? new SemanticSurfaceDisposition(SemanticSurfaceDispositionKind.Rejected, SemanticVersionFeatures.MixedReactionEffects)
            : new SemanticSurfaceDisposition(SemanticSurfaceDispositionKind.Rendered));

    /// <summary>
    /// Gets the default Scene disposition for list queries. Composition has no diagnostic channel;
    /// only optional snapshot lookups have a default query input form, not collection results.
    /// </summary>
    public static SemanticSurfaceDisposition DefaultSceneListQueries { get; } = new(
        SemanticSurfaceDispositionKind.Ignored,
        "Default Scene omits keyed and unkeyed list queries; collection result composition is not supported.");

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
        Add(entries, "ExecutableSemanticModel", ignored("Compiler-owned deprecation warnings (PLAY0445), not executable projection behavior."), "DeprecationDiagnostics");
        Add(entries, "LanguageVersion", rendered, "Major Minor");
        Add(entries, "SemanticVersion", rendered, "Major Minor");
        Add(entries, "SemanticRevision", ignored("Canonical model revision, not an application artifact."), "IsSet");
        Add(entries, "SemanticId", ignored("Identity validity is guaranteed by Screenplay; identity indexes references rather than source members."), "IsSet");
        Add(entries, "EventContractId", ignored("Stable contract identity is already validated by Screenplay; the generated event uses its name."), "IsSet");
        Add(entries, "EventContractRevision", ignored("Revision validity and lineage ordering are guaranteed by Screenplay."), "IsValid");
        Add(entries, "EventContractRevision", ignored("Revision values are carried for admission; evolved events fail STAGE-ESM-026, not generation registration."), "Value");
        Add(entries, "SemanticApplication", rendered, "Concepts Id Modules Name Types Policies EventSources");
        Add(entries, "SemanticApplication", rejected(SemanticVersionFeatures.AtomicReactionBatches), "ReactionProductionMode");
        Add(entries, "SemanticReactionProductionMode", rejected(SemanticVersionFeatures.AtomicReactionBatches), "Atomic");
        Add(entries, "SemanticModule", rendered, "Features Id Name");
        Add(entries, "SemanticFeature", rendered, "Features Id Name Slices");
        Add(entries, "SemanticSlice", rendered, "Commands Constraints Events Id Kind Name Projections Queries ReadModels Specifications Reducers Reactions");
        Add(entries, "SemanticReducer", rendered, "Name ReadModel Transitions Key InitialState Result");
        Add(entries, "SemanticReducerTransition", rendered, "EventContract RequirementId");
        Add(entries, "SemanticReducerKey", rendered, "EventSourceId");
        Add(entries, "SemanticReducerResult", rendered, "StateOrDelete");
        Add(entries, "SemanticSliceKind", rendered, "StateChange StateView Automation");
        Add(entries, "SemanticSliceKind", rejected("STAGE-ESM-001"), "Unknown");

        // Types, declarations and validation. Properties carry type and identifier semantics into
        // generated records, event destinations, projection keys and specification values.
        Add(entries, "SemanticConcept", rendered, "Id Name Primitive Validations Values Numbers Compliance Generation");
        Add(entries, "SemanticGeneration", rendered, "Kind Prefix Case Size Alphabet Count Encoding");

        Add(entries, "SemanticGeneration", rendered, "Pattern Checksum Minimum Maximum");

        // Rule generation emits the verified body through the Fundamentals context factory.
        Add(entries, "SemanticGeneration", rendered, "RequirementId");
        Add(entries, "SemanticConceptCompliance", rendered, "Personal Secret");
        Add(entries, "SemanticSecret", rendered, "Scope");
        Add(entries, "SemanticPersonalData", ignored("GDPR qualifiers only travel via opt-in [ComplianceDetails] (#259)."), "Special Criminal");
        Add(entries, "SemanticCompositeType", rendered, "Id Name Properties");
        Add(entries, "SemanticProperty", rendered, "Id IsIdentifier Name Type IsGenerated");
        Add(entries, "SemanticTypeReference", rendered, "IsCollection IsOptional Kind Primitive Target");
        Add(entries, "SemanticTypeReferenceKind", rendered, "Primitive Concept CompositeType");
        Add(entries, "SemanticTypeReferenceKind", rejected("STAGE-ESM-003"), "Unknown");
        Add(entries, "SemanticPrimitiveType", rendered, "Uuid Text WholeNumber DecimalNumber Boolean Date DateTime");
        Add(entries, "SemanticPrimitiveType", rejected("STAGE-ESM-002"), "Unknown");

        // Message includes localized $strings keys only with a validated catalog; missing default keys block as STAGE-ESM-018.
        Add(entries, "SemanticValidationRule", rendered, "Kind Message Property Severity Operand");
        Add(entries, "SemanticValidationRule", rendered, "Name RequirementId");
        Add(entries, "SemanticValidationRuleKind", rendered, "NotEmpty Maximum Minimum Equal NotEqual GreaterThan GreaterThanOrEqual LessThan LessThanOrEqual Length AllGreaterThan AllGreaterThanOrEqual Matches Checksum Contains NotContains StartsWith EndsWith RulePredicate CodeValidation");
        Add(entries, "SemanticValidationRuleKind", rejected("STAGE-ESM-005"), "Unknown");
        Add(entries, "SemanticValidationSeverity", rendered, "Error Information Warning");

        // State change: one or more commands with mapped events and portable When conditions in declaration order. Tags preserve
        // contract-then-production order; duplicate tags fail admission because Chronicle deduplicates.
        // Requirements use the same catalog-backed message resolution as property and concept validation.
        Add(entries, "SemanticCommand", rendered, "Id Name Properties Validations Produces Destination Requirements Authorization");
        Add(entries, "SemanticCommand", rendered, "Route");
        Add(entries, "SemanticCommand", rejected(SemanticVersionFeatures.PendingCommandHandler), "Handler");
        Add(entries, "SemanticCommandHandler", rejected(SemanticVersionFeatures.PendingCommandHandler), "RequirementId ResultVersion");
        Add(entries, "SemanticCommand", rendered, "CodeValidations");
        Add(entries, "SemanticCodeValidation", rendered, "RequirementId");
        Add(entries, "SemanticCommandRoute", rendered, "Source Stream StreamId StreamIdParts");
        Add(entries, "SemanticCommandRoutePart", rendered, "Part Value");
        Add(entries, "SemanticEventSource", rendered, "Id IdentifierType Name SourceKind Streams");
        Add(entries, "SemanticEventStream", rendered, "Id Name StreamIdParts StreamIdType StreamKind");
        Add(entries, "SemanticStreamIdPart", rendered, "Name Type");

        // The render request now carries compiler requirements and resolved bodies by requirement id.
        // Their content hashes are checked before rendering. Pure reducer transitions and whole opaque policies are
        // admitted through the closed-compilation pure gate; validation bodies refuse ambient context reads.
        Add(entries, "SemanticImplementationRequirement", ignored("Compiler metadata is supplied with the request and content hashes are verified; opaque owning behavior still fails admission."), "Role Owner Member Language File ContentHash Source RequirementId ContextVersion ResultVersion RequiredCapability AttachmentResolution BodySpan BodyLines");
        Add(entries, "SemanticImplementationRequirement", rejected(SemanticVersionFeatures.PendingCommandHandler), "Hints IsProvisional IsAmbiguous");
        Add(entries, "SemanticAttachmentResolution", ignored("Resolved and unresolved attachments retain whole-envelope verification (STAGE-ESM-020)."), "Resolved UnresolvedFile");
        Add(entries, "SemanticAttachmentResolution", rejected(SemanticVersionFeatures.PendingCommandHandler), "Pending");
        Add(entries, "SemanticExecutionCapability", rejected(SemanticVersionFeatures.PendingCommandHandler), "ImplementationBody");
        Add(entries, "SemanticProducedEvent", rendered, "Destination EventContract Mappings Tags When");
        Add(entries, "SemanticProducedEvent", rejected("STAGE-ESM-006"), "Condition");
        Add(entries, "SemanticEventContract", rendered, "Id Name StoredName Properties Tags");
        Add(entries, "SemanticEventContract", ignored("Carried revision selects admission: initial events render unchanged; evolved revisions fail STAGE-ESM-026 until event migrations can render."), "Revision");
        Add(entries, "SemanticEventContract", ignored("The initial event revision's stable contract identity is owned by Screenplay, not emitted by the first renderer."), "ContractId");

        // ESM v4 lineage is carried, not emitted. Selected evolved events and dependencies
        // fail STAGE-ESM-026; historical typed-context references still fail STAGE-ESM-025.
        Add(entries, "SemanticEventContract", ignored("Lineage metadata is carried in the input; no migration or predecessor artifact is emitted."), "Predecessor");
        Add(entries, "SemanticEventContract", ignored("Prior schemas are carried for historical-reference admission (STAGE-ESM-025), never emitted as current events."), "PriorRevisions");
        Add(entries, "SemanticEventRevision", ignored("Historical lineage metadata is carried, not rendered; historical consumption fails STAGE-ESM-025."), "Predecessor Revision");
        Add(entries, "SemanticEventRevision", ignored("Historical property identities detect refused shape references (STAGE-ESM-025); no historical record is emitted."), "Properties");
        Add(entries, "SemanticEventRevision", ignored("Historical tags are carried but never appended with the current generation."), "Tags");
        Add(entries, "SemanticPropertyMapping", rendered, "Source TargetProperty");
        Add(entries, "SemanticStateChangeDestination", rendered, "Type Value");
        Add(entries, "SemanticResolvedExpression", rendered, "Root Source Target");
        Add(entries, "SemanticExpression", rendered, "Kind");
        Add(entries, "SemanticExpressionKind", rendered, "Resolved Refusal");
        Add(entries, "SemanticRefusalExpression", rendered, "Member Type");
        Add(entries, "SemanticExpressionKind", rejected("STAGE-ESM-006"), "Unknown Value EventContext");
        Add(entries, "SemanticExpressionRootKind", rendered, "Command Event");
        Add(entries, "SemanticExpressionRootKind", rejected("STAGE-ESM-006"), "Unknown");
        Add(entries, "SemanticExpressionSourceKind", rendered, "Property");
        Add(entries, "SemanticExpressionSourceKind", rejected("STAGE-ESM-006"), "Unknown");
        Add(entries, "SemanticEventContextExpression", rendered, "Type Value");
        Add(entries, "SemanticEventContextValueKind", rendered, "Occurred");
        Add(entries, "SemanticEventContextValueKind", rejected("STAGE-ESM-013"), "Unknown EventSourceIdentity CausedBySubject CausedByName CausedByUserName");
        Add(entries, "SemanticValueExpression", rejected("STAGE-ESM-006"), "Value");

        // State view: an optional snapshot lookup by identifier or a snapshot/live collection (keyed by one `by` or unkeyed).
        // Query arguments retain concept types, including nested members and collection elements, for Arc validation.
        // QueryShapes records the cross-product rather than treating an enum value as universally supported.
        Add(entries, "SemanticReadModel", rendered, "Id Name Properties");
        Add(entries, "SemanticProjection", rendered, "Id Name ReadModel Transitions");
        Add(entries, "SemanticProjection", rendered, "Scope");
        Add(entries, "SemanticProjectionTransition", rendered, "AffectedInstance EventContract Mappings");
        Add(entries, "SemanticAffectedInstance", rendered, "Cardinality Key");
        Add(entries, "AffectedInstanceCardinality", rendered, "One");
        Add(entries, "AffectedInstanceCardinality", rejected("STAGE-ESM-009"), "Unknown Many ZeroOrOne");
        Add(entries, "SemanticKeyedQuery", rendered, "Argument Cardinality Delivery Id KeyProperty Name ReadModel Authorization Filters");

        // ESM v27 single-instance lookups by key parts need Chronicle to find one instance by a composite key (Chronicle#4265).
        Add(entries, "SemanticKeyedQuery", rejected(SemanticVersionFeatures.KeyPartsLookup), "KeyParts");
        Add(entries, "SemanticQueryKeyPart", rejected(SemanticVersionFeatures.KeyPartsLookup), "Id Name Type KeyProperty");
        Add(entries, "SemanticQueryFilter", rendered, "Id Name Type Property");
        Add(entries, "SemanticQueryFilterValue", rendered, "Filter Value");
        Add(entries, "SemanticReadModelQueryArgument", rendered, "Id Name Type");
        Add(entries, "SemanticQueryCardinality", rendered, "ZeroOrOne Many");
        Add(entries, "SemanticQueryCardinality", rejected("STAGE-ESM-010"), "Unknown One");
        Add(entries, "SemanticQueryDelivery", rendered, "Snapshot Live");
        Add(entries, "SemanticQueryDelivery", rejected("STAGE-ESM-010"), "Unknown");

        // Supported scoped blocks render; conflicting roles and nested from without a matching root
        // from and identical key fail STAGE-ESM-017. Composite type keys remain rejected; read models with several key parts render through parts keys, but Stage
        // cannot issue single-instance lookups by key parts (Chronicle#4265). Every literals lower to from
        // Set mappings only at join-free levels (Chronicle#4663); prefix overlaps and non-Set collisions remain refused. All literals and
        // text literals outside Chronicle's fluent $value grammar, expression-like event-property
        // names (including derived functions), unsafe read-model property paths and camel-case
        // collisions, and unvalidated $eventSourceId mapping targets also fail STAGE-ESM-017.
        // Identity-only all renders as FromAll; literals in all remain rejected (Chronicle#4663), because
        // lowering onto from blocks would miss unrelated event types and sources.
        // Child join removal across parents and nested clear/recreation render on Chronicle 19.32.0;
        // differential and MongoDB probes cover both sequences. Root join removal remains blocked by
        // Chronicle#4263 and Screenplay#563; joins/children inside nested by Chronicle#4125. Nested clear requires a matching root from
        // and identical key.
        Add(entries, "SemanticProjectionScope", rendered, "Children From Joins Every JoinRemovals Nested Removals");
        Add(entries, "SemanticProjectionChildren", rendered, "IdentifiedBy Property Scope");
        Add(entries, "SemanticProjectionCompositeKey", rejected("STAGE-ESM-017"), "Parts Type");
        Add(entries, "SemanticProjectionEventContextValue", rejected("STAGE-ESM-017"), "Path");
        Add(entries, "SemanticProjectionEventProperty", rendered, "Path");
        Add(entries, "SemanticProjectionEvery", rendered, "IncludeChildren Mappings SubscribesToAllEvents");
        Add(entries, "SemanticProjectionFrom", rendered, "EventContract Key Mappings ParentKey");
        Add(entries, "SemanticProjectionJoin", rendered, "EventContract Mappings On");
        Add(entries, "SemanticProjectionJoin", rejected("STAGE-ESM-017"), "Key");
        Add(entries, "SemanticProjectionJoinRemoval", rendered, "EventContract Key");
        Add(entries, "SemanticProjectionKey", rendered, "Kind");
        Add(entries, "SemanticProjectionKeyKind", rendered, "Value");
        Add(entries, "SemanticProjectionKeyKind", rejected("STAGE-ESM-017"), "Unknown Composite");

        // ESM v27 read models with several key parts render a key record, an Id property of it, and Chronicle's UsingCompositeKey
        // projection keys, for scoped from and remove-with blocks; any other composite shape fails STAGE-ESM-048.
        Add(entries, "SemanticProjectionKeyKind", rendered, "Parts");
        Add(entries, "SemanticProjectionPartsKey", rendered, "Parts");
        Add(entries, "SemanticProjectionKeyPart", rendered, "Property Value");
        Add(entries, "SemanticProjectionLiteral", rendered, "Value");
        Add(entries, "SemanticProjectionMapping", rendered, "Operation Source Target");
        Add(entries, "SemanticProjectionNested", rendered, "Property Scope");
        Add(entries, "SemanticProjectionOperation", rendered, "Set Add Subtract Increment Decrement Clear");
        Add(entries, "SemanticProjectionOperation", rejected("STAGE-ESM-017"), "Unknown");
        Add(entries, "SemanticProjectionRemoval", rendered, "EventContract Key ParentKey");
        Add(entries, "SemanticProjectionValue", rendered, "Kind");
        Add(entries, "SemanticProjectionValueKey", rendered, "Value");
        Add(entries, "SemanticProjectionValueKind", rendered, "EventProperty EventSourceIdentity Literal");
        Add(entries, "SemanticProjectionValueKind", rejected("STAGE-ESM-017"), "Unknown EventContext");

        // Specifications render command actions with exact scalar fixtures and supported outcomes.
        // Scoped read-model/query expectations replay every produced event in production order;
        // incomplete or ambiguous unordered duplicate replay fails STAGE-ESM-011. Unordered event
        // assertions compare the produced stream even when the expected fact omits its source.
        // ESM v18 query-only validation errors and empty results execute through Arc QueryScenario; application views still require a projection or reducer.
        // Protected query results and query-only denials execute through Arc QueryScenario with fixture principals;
        // live collections render as Observe subjects and snapshots as Task collection reads; protected collection success
        // specifications use QueryScenario's default Chronicle observations. Live single-result queries remain rejected by STAGE-ESM-010. Given events that violate an admitted constraint
        // fail STAGE-ESM-011 before log seeding. An unauthenticated caller fixture with roles or claims
        // also fails STAGE-ESM-011 before rendering: Arc supplies an empty guest principal.
        Add(entries, "SemanticSpecification", rendered, "Id Name GivenEvents GivenReadModels GivenCaller ThenEvents ThenEventsInAnyOrder ThenReadModels ThenQueries ThenErrors ThenDenied When");

        // ESM v5 keyed read-model absence fails STAGE-ESM-027: the generated ReadModelScenario exposes only a materialized record.
        Add(entries, "SemanticSpecification", rejected("STAGE-ESM-027"), "ThenAbsentReadModels");

        Add(entries, "SemanticSpecificationAbsentReadModel", rejected("STAGE-ESM-027"), "Key ReadModel");
        Add(entries, "SemanticSpecification", rendered, "WhenAppended ThenNoEvents");

        // Redelivery admits one named trigger with portable history; cascades and refusal branches fail STAGE-ESM-011.
        Add(entries, "SemanticSpecification", rendered, "WhenRedelivered");
        Add(entries, "SemanticSpecificationRedelivery", rendered, "Reaction Given");
        Add(entries, "SemanticSpecificationCommand", rendered, "Command Values EventSource");
        Add(entries, "SemanticSpecificationAppend", rendered, "EventContract EventSource Values Route");
        Add(entries, "SemanticSpecificationEvent", rendered, "EventContract EventSource Values");
        Add(entries, "SemanticSpecificationEvent", rendered, "Route Unrouted");
        Add(entries, "SemanticFixtureRoute", rendered, "Source Stream StreamId StreamIdParts");
        Add(entries, "SemanticFixtureRoutePart", rendered, "Part Value");
        Add(entries, "SemanticSpecificationReadModel", rendered, "Key ReadModel Values");
        Add(entries, "SemanticSpecificationReadModel", rendered, "Exactly");
        Add(entries, "SemanticSpecificationQueryResult", rendered, "Key Query Results Exactly Filters");
        Add(entries, "SemanticSpecificationError", rendered, "Message");
        Add(entries, "SemanticSpecificationError", rejected("STAGE-ESM-011"), "Code"); // Codes naming a rendered constraint are admitted.
        Add(entries, "SemanticPropertyValue", rendered, "TargetProperty Value");
        Add(entries, "SemanticEventSourceIdentity", rendered, "Type Value");
        Add(entries, "SemanticValue", rendered, "Kind");
        Add(entries, "SemanticTextValue", rendered, "Value");

        // Whole-number values retain int32 output below v8; out-of-range literals fail STAGE-ESM-031.
        // At v8 and later their declarations, sentinels and literals render as long.
        Add(entries, "SemanticNumberValue", rendered, "Value");
        Add(entries, "SemanticBooleanValue", rendered, "Value");
        Add(entries, "SemanticArrayValue", rendered, "Values");
        Add(entries, "SemanticCompositeValue", rejected("STAGE-ESM-011"), "Properties");
        Add(entries, "SemanticValueKind", rendered, "Null Text Number Boolean Array");
        Add(entries, "SemanticValueKind", rejected("STAGE-ESM-011"), "Unknown Composite");
        Add(entries, "SemanticNullValue", rendered, "$type");

        // Portable authorization runs through Arc. A whole opaque csharp/file policy renders as its verified pure body
        // over a typed PolicyContext (Subject, Identity, Occurred = Arc's AuthorizationPolicyContext.ReceivedAt); one that
        // reads Tenant, the dynamic Artifact or an unmapped Identity member, or is nested in a condition, fails STAGE-ESM-015.
        // Specifications reaching an opaque policy fail STAGE-ESM-011: the reference evaluator returns Unsupported.
        // Caller fixtures and command denial are checked by Arc's pipeline.
        // Role claim URIs cannot preserve the separate Screenplay roles/claims boundary (011/015).
        // Command-property requirements render as validator rules.
        Add(entries, "SemanticAuthenticatedCondition", rendered, "$type");
        Add(entries, "SemanticAuthorization", rendered, "$type");
        Add(entries, "SemanticCondition", rendered, "$type");
        Add(entries, "SemanticPolicyCondition", rendered, "$type");
        Add(entries, "SemanticOpaquePolicyCondition", rendered, "RequirementId");

        // ESM v7 policy negation renders three-valued: a claim comparison with a missing, null or non-text target is
        // unknown, negation keeps it unknown, and a policy whose result is unknown denies.
        Add(entries, "SemanticNotPolicyCondition", rendered, "Operand");
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

        // Screenplay 4.61 added ESM v6 reactions, captures, application triggers and clock/capture
        // specifications. Event reactions and their Automation slice artifacts render as Chronicle reactors.
        // Non-event triggers and opaque bodies still refuse with STAGE-ESM-024; mixed handler results fail 037.
        // Conditions, mappings and destinations outside the faithful target subset fail STAGE-ESM-039.
        var v6 = rejected(SemanticVersionFeatures.Automation);
        Add(entries, "SemanticApplication", v6, "Triggers");
        Add(entries, "SemanticApplicationTrigger", v6, "Id Name Properties");
        Add(entries, "SemanticSlice", v6, "Captures");
        Add(entries, "SemanticSliceKind", v6, "Translate");
        Add(entries, "SemanticExpressionRootKind", v6, "Trigger");
        Add(entries, "SemanticProducedEvent", rendered, "DestinationType");
        Add(entries, "SemanticInvocation", rendered, "Command Mappings OnRefused");
        Add(entries, "SemanticInvocationRefusal", rendered, "Selector Constraint Produces");
        Add(entries, "SemanticReaction", rendered, "Id Name Triggers");
        Add(entries, "SemanticReactionTrigger", rendered, "Invokes Kind Produces Source Where");
        Add(entries, "SemanticReactionTrigger", v6, "At Every OnDayOfMonth OnDayOfWeek RequirementId");
        Add(entries, "SemanticReactionTriggerKind", rendered, "Event");
        Add(entries, "SemanticReactionTriggerKind", v6, "Unknown ApplicationTrigger Interval Schedule Shutdown Startup");
        Add(entries, "SemanticCapture", v6, "Appends Children Id Key Map Name Nested");
        Add(entries, "SemanticCaptureAppend", v6, "EventContract EventSourceType Mappings Tags When");
        Add(entries, "SemanticCaptureChildren", v6, "Appends Field IdentifiedBy Map");
        Add(entries, "SemanticCaptureNested", v6, "Appends Field Map");
        Add(entries, "SemanticCaptureRecord", v6, "Fields");
        Add(entries, "SemanticCaptureField", v6, "Kind Name Record Records Value");
        Add(entries, "SemanticCaptureFieldKind", v6, "Unknown Record Records Value");
        Add(entries, "SemanticCaptureCondition", v6, "Expression Fields From Kind To");
        Add(entries, "SemanticCaptureConditionKind", v6, "Unknown Added AllChanged AnyChanged Expression Removed Transition");
        Add(entries, "SemanticCaptureMap", v6, "Kind Separator Source Targets Template Translations");
        Add(entries, "SemanticCaptureMapKind", v6, "Unknown Split Template Value");
        Add(entries, "SemanticCaptureMapping", v6, "Field TargetProperty Value");
        Add(entries, "SemanticCaptureTemplatePart", v6, "Field Text");
        Add(entries, "SemanticCaptureTranslation", v6, "From To");
        Add(entries, "SemanticSpecification", v6, "GivenCaptures GivenClock WhenCapture WhenClock WhenTrigger");
        Add(entries, "SemanticSpecificationCapture", v6, "Capture Record");
        Add(entries, "SemanticSpecificationTrigger", v6, "Kind Trigger Values");

        // ESM v7 generated UUID concepts and responses render through Arc's generated-value and response contracts.
        // Admission still refuses non-UUID generation and combinations the target cannot preserve.
        var generated = rendered;
        Add(entries, "SemanticSpecificationCommand", generated, "GeneratedValues");
        var responses = rendered;
        Add(entries, "SemanticCommand", responses, "Response");
        Add(entries, "SemanticCommandResponse", responses, "$type");
        Add(entries, "SemanticScalarCommandResponse", responses, "Source Type");
        Add(entries, "SemanticRecordCommandResponse", responses, "Fields");
        Add(entries, "SemanticCommandResponseField", responses, "Name Source Type");
        Add(entries, "SemanticSpecification", responses, "ThenReturns");
        Add(entries, "SemanticSpecificationResponse", responses, "$type");
        Add(entries, "SemanticScalarSpecificationResponse", responses, "Value");
        Add(entries, "SemanticRecordSpecificationResponse", responses, "Fields");
        Add(entries, "SemanticSpecificationResponseField", responses, "Name Value");

        // Screenplay 4.117 added ESM v9: public events and their origin store, translation direction, projections and
        // reducers that target a slice's public event, and `source events` captures. Each is selected only by use, so a
        // v1–v8 model carries the default - a private local event, a read-model target, no direction - which is exactly
        // what Stage renders. Admit v9 by version, but refuse publication, foreign consumption and translation separately.
        var publication = rejected(SemanticVersionFeatures.PublicEvents);
        var foreign = rejected(SemanticVersionFeatures.ForeignEvents);
        Add(entries, "SemanticEventContract", publication, "Visibility");
        Add(entries, "SemanticEventContract", foreign, "Origin");
        Add(entries, "SemanticEventVisibility", rendered, "Private");
        Add(entries, "SemanticEventVisibility", publication, "Public");
        Add(entries, "SemanticProjection", publication, "Target");
        Add(entries, "SemanticReducer", publication, "Target");
        Add(entries, "SemanticProjectionTargetKind", rendered, "ReadModel");
        Add(entries, "SemanticProjectionTargetKind", publication, "Event");
        Add(entries, "SemanticSlice", v6, "Direction");
        Add(entries, "SemanticCapture", foreign, "EventsSource");
        Add(entries, "SemanticCaptureEventsSource", foreign, "Events");

        // Screenplay 4.125 added ESM v10: reaction execution identity, observer filters and production routes.
        // Admit each construct only on the paths that can preserve it.
        Add(entries, "SemanticReaction", rendered, "RunsAs");
        Add(entries, "SemanticReactionIdentity", rendered, "Kind Roles");
        Add(entries, "SemanticReactionIdentityKind", rendered, "System");
        Add(entries, "SemanticReaction", rendered, "From");

        // Observer filters render as Chronicle attributes. Reducers retain STAGE-ESM-035 in the
        // Host and executor: neither executes reducer bodies.
        Add(entries, "SemanticReducer", rendered, "From");
        Add(entries, "SemanticObserverFilter", rendered, "Source Stream");
        Add(entries, "SemanticProducedEvent", rejected(SemanticVersionFeatures.ProductionRoutes), "Route");

        // ESM v25 protected command reads render as Arc read protection; the shapes the target cannot preserve
        // are refused case by case in SemanticCratisAdmission with STAGE-ESM-044, each naming its reason.
        var reads = rendered;
        Add(entries, "SemanticApplication", reads, "CommandReadMode");
        Add(entries, "SemanticCommandReadMode", reads, "Protected");
        Add(entries, "SemanticCommand", reads, "Reads");
        Add(entries, "SemanticDecisionRead", reads, "Name ReadModel Key Presence");
        Add(entries, "SemanticDecisionReadPresence", reads, "Required");
        Add(entries, "SemanticConditionOperand", reads, "Read");
        Add(entries, "SemanticExpressionKind", reads, "ReadPath");
        Add(entries, "SemanticReadPathExpression", reads, "Read PropertyId");

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
