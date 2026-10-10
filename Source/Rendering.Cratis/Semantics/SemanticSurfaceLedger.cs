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
/// Inventories the executable semantic surface audited against Screenplay 4.114.0; ESM v1–v8 version pairs are admitted, but evolved events require migration rendering
/// and each v5–v7 construct Stage does not render yet refuses the model with its own diagnostic.
/// A rejected member names the admission diagnostic that blocks its unsupported shape.
/// </summary>
internal static class SemanticSurfaceLedger
{
    /// <summary>
    /// Gets the dispositions keyed by TypeName.MemberName.
    /// </summary>
    public static IReadOnlyDictionary<string, SemanticSurfaceDisposition> Entries { get; } = Build();

    /// <summary>
    /// Gets the query shape matrix. Snapshot optional lookups require a key; live collections may be keyed or unkeyed.
    /// Other cardinality/delivery/key combinations fail STAGE-ESM-010 before rendering.
    /// </summary>
    public static IReadOnlyDictionary<(SemanticQueryCardinality Cardinality, SemanticQueryDelivery Delivery, bool Keyed), SemanticSurfaceDisposition> QueryShapes { get; } =
        (from cardinality in Enum.GetValues<SemanticQueryCardinality>()
         from delivery in Enum.GetValues<SemanticQueryDelivery>()
         from keyed in new[] { false, true }
         select (Cardinality: cardinality, Delivery: delivery, Keyed: keyed))
        .ToDictionary(shape => shape, shape =>
            (shape.Cardinality == SemanticQueryCardinality.ZeroOrOne && shape.Delivery == SemanticQueryDelivery.Snapshot && shape.Keyed) ||
            (shape.Cardinality == SemanticQueryCardinality.Many && shape.Delivery == SemanticQueryDelivery.Live)
                ? new SemanticSurfaceDisposition(SemanticSurfaceDispositionKind.Rendered)
                : new SemanticSurfaceDisposition(SemanticSurfaceDispositionKind.Rejected, "STAGE-ESM-010"));

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
        Add(entries, "SemanticApplication", rendered, "Concepts Id Modules Name Types Policies");
        Add(entries, "SemanticApplication", rendered, "EventSources");
        Add(entries, "SemanticModule", rendered, "Features Id Name");
        Add(entries, "SemanticFeature", rendered, "Features Id Name Slices");
        Add(entries, "SemanticSlice", rendered, "Commands Constraints Events Id Kind Name Projections Queries ReadModels Specifications");
        Add(entries, "SemanticSlice", rendered, "Reducers");
        Add(entries, "SemanticReducer", rendered, "Name ReadModel Transitions Key InitialState Result");
        Add(entries, "SemanticReducerTransition", rendered, "EventContract RequirementId");
        Add(entries, "SemanticReducerKey", rendered, "EventSourceId");
        Add(entries, "SemanticReducerResult", rendered, "StateOrDelete");
        Add(entries, "SemanticSliceKind", rendered, "StateChange StateView");
        Add(entries, "SemanticSliceKind", rejected("STAGE-ESM-001"), "Unknown");

        // Types, declarations and validation. Properties carry type and identifier semantics into
        // generated records, event destinations, projection keys and specification values.
        Add(entries, "SemanticConcept", rendered, "Id Name Primitive Validations Values");
        Add(entries, "SemanticCompositeType", rendered, "Id Name Properties");
        Add(entries, "SemanticProperty", rendered, "Id IsIdentifier Name Type");
        Add(entries, "SemanticProperty", rejected("STAGE-ESM-028"), "IsGenerated");
        Add(entries, "SemanticTypeReference", rendered, "IsCollection IsOptional Kind Primitive Target");
        Add(entries, "SemanticTypeReferenceKind", rendered, "Primitive Concept CompositeType");
        Add(entries, "SemanticTypeReferenceKind", rejected("STAGE-ESM-003"), "Unknown");
        Add(entries, "SemanticPrimitiveType", rendered, "Uuid Text WholeNumber DecimalNumber Boolean Date DateTime");
        Add(entries, "SemanticPrimitiveType", rejected("STAGE-ESM-002"), "Unknown");

        // Message includes localized $strings keys only with a validated catalog; missing default keys block as STAGE-ESM-018.
        Add(entries, "SemanticValidationRule", rendered, "Kind Message Property Severity Operand");
        Add(entries, "SemanticValidationRule", rejected("STAGE-ESM-005"), "Name RequirementId");
        Add(entries, "SemanticValidationRuleKind", rendered, "NotEmpty Maximum Minimum Equal NotEqual GreaterThan GreaterThanOrEqual LessThan LessThanOrEqual Length AllGreaterThan AllGreaterThanOrEqual Matches");
        Add(entries, "SemanticValidationRuleKind", rejected("STAGE-ESM-005"), "Unknown RulePredicate CodeValidation");
        Add(entries, "SemanticValidationSeverity", rendered, "Error Information Warning");

        // State change: one command with unconditional mapped events in declaration order. Tags preserve
        // contract-then-production order; duplicate tags fail admission because Chronicle deduplicates.
        // Requirements use the same catalog-backed message resolution as property and concept validation.
        Add(entries, "SemanticCommand", rendered, "Id Name Properties Validations Produces Destination Requirements Authorization");
        Add(entries, "SemanticCommand", rendered, "Route");
        Add(entries, "SemanticCommand", rejected("STAGE-ESM-005"), "CodeValidations");
        Add(entries, "SemanticCodeValidation", rejected("STAGE-ESM-005"), "RequirementId");
        Add(entries, "SemanticCommandRoute", rendered, "Source Stream StreamId StreamIdParts");
        Add(entries, "SemanticCommandRoutePart", rendered, "Part Value");
        Add(entries, "SemanticEventSource", rendered, "Id IdentifierType Name SourceKind Streams");
        Add(entries, "SemanticEventStream", rendered, "Id Name StreamIdParts StreamIdType StreamKind");
        Add(entries, "SemanticStreamIdPart", rendered, "Name Type");

        // The render request now carries compiler requirements and resolved bodies by requirement id.
        // Their content hashes are checked before rendering. Pure reducer transitions and whole opaque policies are
        // admitted through the closed-compilation pure gate; opaque validation still cannot receive RuleContext.Occurred.
        Add(entries, "SemanticImplementationRequirement", ignored("Compiler metadata is supplied with the request and content hashes are verified; opaque owning behavior still fails admission."), "Role Owner Member Language File ContentHash Source RequirementId ContextVersion ResultVersion RequiredCapability AttachmentResolution BodySpan BodyLines");
        Add(entries, "SemanticProducedEvent", rendered, "Destination EventContract Mappings Tags");
        Add(entries, "SemanticProducedEvent", rejected("STAGE-ESM-006"), "Condition When");
        Add(entries, "SemanticEventContract", rendered, "Id Name Properties Tags");
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
        Add(entries, "SemanticExpressionKind", rendered, "Resolved");
        Add(entries, "SemanticExpressionKind", rejected("STAGE-ESM-006"), "Unknown Value EventContext");
        Add(entries, "SemanticExpressionRootKind", rendered, "Command Event");
        Add(entries, "SemanticExpressionRootKind", rejected("STAGE-ESM-006"), "Unknown");
        Add(entries, "SemanticExpressionSourceKind", rendered, "Property");
        Add(entries, "SemanticExpressionSourceKind", rejected("STAGE-ESM-006"), "Unknown");
        Add(entries, "SemanticEventContextExpression", rendered, "Type Value");
        Add(entries, "SemanticEventContextValueKind", rendered, "Occurred");
        Add(entries, "SemanticEventContextValueKind", rejected("STAGE-ESM-013"), "Unknown EventSourceIdentity CausedBySubject CausedByName CausedByUserName");
        Add(entries, "SemanticValueExpression", rejected("STAGE-ESM-006"), "Value");

        // State view: an optional snapshot lookup by identifier or a live collection (keyed by one `by` or unkeyed).
        // QueryShapes records the cross-product rather than treating an enum value as universally supported.
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
        Add(entries, "SemanticQueryCardinality", rendered, "ZeroOrOne Many");
        Add(entries, "SemanticQueryCardinality", rejected("STAGE-ESM-010"), "Unknown One");
        Add(entries, "SemanticQueryDelivery", rendered, "Snapshot Live");
        Add(entries, "SemanticQueryDelivery", rejected("STAGE-ESM-010"), "Unknown");

        // Supported scoped blocks render; conflicting roles and nested from without a matching root
        // from and identical key fail STAGE-ESM-017. Composite keys remain rejected because Stage
        // cannot issue keyed lookups for composite read models (Chronicle#4265). Every literals lower to from
        // Set mappings only at join-free levels (Chronicle#4663); prefix overlaps and non-Set collisions remain refused. FromAll (Chronicle#4266) and
        // text literals outside Chronicle's fluent $value grammar, expression-like event-property
        // names (including derived functions), unsafe read-model property paths and camel-case
        // collisions, and unvalidated $eventSourceId mapping targets also fail STAGE-ESM-017.
        // FromAll stays rejected (Chronicle#4266): MongoDB does not materialize an unrelated source observed in memory.
        // Child join removal across parents and nested clear/recreation render on Chronicle 19.32.0;
        // differential and MongoDB probes cover both sequences. Root join removal remains blocked by
        // Chronicle#4263 and Screenplay#563; joins/children inside nested by Chronicle#4125. Nested clear requires a matching root from
        // and identical key.
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
        Add(entries, "SemanticProjectionJoinRemoval", rendered, "EventContract Key");
        Add(entries, "SemanticProjectionKey", rendered, "Kind");
        Add(entries, "SemanticProjectionKeyKind", rendered, "Value");
        Add(entries, "SemanticProjectionKeyKind", rejected("STAGE-ESM-017"), "Unknown Composite");
        Add(entries, "SemanticProjectionKeyPart", rejected("STAGE-ESM-017"), "Property Value");
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
        // Protected query results and query-only denials execute through Arc QueryScenario with fixture principals;
        // live collections render as collection reads; live single-result queries remain rejected by STAGE-ESM-010. Given events that violate an admitted constraint
        // fail STAGE-ESM-011 before log seeding. An unauthenticated caller fixture with roles or claims
        // also fails STAGE-ESM-011 before rendering: Arc supplies an empty guest principal.
        Add(entries, "SemanticSpecification", rendered, "Id Name GivenEvents GivenReadModels GivenCaller ThenEvents ThenEventsInAnyOrder ThenReadModels ThenQueries ThenErrors ThenDenied When");

        // ESM v5 keyed read-model absence fails STAGE-ESM-027: the generated ReadModelScenario exposes only a materialized record.
        Add(entries, "SemanticSpecification", rejected("STAGE-ESM-027"), "ThenAbsentReadModels");
        Add(entries, "SemanticSpecificationAbsentReadModel", rejected("STAGE-ESM-027"), "Key ReadModel");
        Add(entries, "SemanticSpecification", rejected("STAGE-ESM-011"), "WhenAppended");
        Add(entries, "SemanticSpecificationCommand", rendered, "Command Values EventSource");
        Add(entries, "SemanticSpecificationAppend", rejected("STAGE-ESM-011"), "EventContract EventSource Values");
        Add(entries, "SemanticSpecificationAppend", rejected("STAGE-ESM-030"), "Route");
        Add(entries, "SemanticSpecificationEvent", rendered, "EventContract EventSource Values");
        Add(entries, "SemanticSpecificationEvent", rejected("STAGE-ESM-030"), "Route Unrouted");
        Add(entries, "SemanticFixtureRoute", rejected("STAGE-ESM-030"), "Source Stream StreamId StreamIdParts");
        Add(entries, "SemanticFixtureRoutePart", rejected("STAGE-ESM-030"), "Part Value");
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
        // specifications. The version gate admits v6, but each of these refuses the model with STAGE-ESM-024 (Stage#79).
        var v6 = rejected("STAGE-ESM-024");
        Add(entries, "SemanticApplication", v6, "Triggers");
        Add(entries, "SemanticApplicationTrigger", v6, "Id Name Properties");
        Add(entries, "SemanticSlice", v6, "Captures Reactions");
        Add(entries, "SemanticSliceKind", v6, "Automation Translate");
        Add(entries, "SemanticExpressionRootKind", v6, "Trigger");
        Add(entries, "SemanticProducedEvent", v6, "DestinationType");
        Add(entries, "SemanticInvocation", v6, "Command Mappings");
        Add(entries, "SemanticReaction", v6, "Id Name Triggers");
        Add(entries, "SemanticReactionTrigger", v6, "At Every Invokes Kind OnDayOfMonth OnDayOfWeek Produces RequirementId Source Where");
        Add(entries, "SemanticReactionTriggerKind", v6, "Unknown ApplicationTrigger Event Interval Schedule Shutdown Startup");
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

        // Screenplay 4.68 added ESM v7 generated command values and command responses (Stage#175). Each refuses
        // the model before any artifact is planned; nothing is dropped from the request, proxy or specification.
        var generated = rejected("STAGE-ESM-028");
        Add(entries, "SemanticSpecificationCommand", generated, "GeneratedValues");
        var responses = rejected("STAGE-ESM-029");
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
