// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Security.Claims;
using System.Text.RegularExpressions;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Contracts.Execution;
using Cratis.Stage.Contracts.Specifications.Semantic;
using Cratis.Stage.Rendering.Cratis.Semantics;
using Cratis.Stage.Rendering.Cratis.Semantics.Policies;
using Cratis.Stage.Rendering.Cratis.Semantics.Projections;
using Cratis.Stage.Specifications.Commands;

namespace Cratis.Stage.Semantics;

/// <summary>
/// Checks the entire reachable behavior before allowing a specification to execute.
/// </summary>
public static partial class SemanticRunAdmission
{
    /// <summary>
    /// The code for graph declarations and graph projections, which the live runtime registers with the kernel for its graph sink to project, and decides specifications for with the reference graph runner.
    /// </summary>
    public const string GraphsNotProjected = "STAGE-ESM-065";

    static readonly ConditionalWeakTable<ExecutableSemanticModel, ConcurrentDictionary<string, EsmSupportStanding>> _standings = [];
    static readonly string[] _clockMemberPrefixes = ["SemanticCommand.Calendar", "SemanticCommandCalendar.", "SemanticClockOperand.", "SemanticClockKind.", "SemanticClockUnit.", "SemanticConditionOperand.Clock", "SemanticConditionOperand.DateOf", "SemanticValidationRule.ClockOperand", "SemanticSpecification.GivenTimeZone"];

    [GeneratedRegex(@"\.Commands\[(?<index>\d+)\]", RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture, matchTimeoutMilliseconds: 1000)]
    private static partial Regex CommandSegment { get; }

    /// <summary>
    /// Checks the audited version pair before finding constructs that block live model admission.
    /// </summary>
    /// <param name="model">The executable model.</param>
    /// <returns>The precise capability refusals.</returns>
    public static IEnumerable<SemanticAdmissionFeature> ModelFeatures(ExecutableSemanticModel model) => ModelFeatures(model, _ => true);

    /// <summary>
    /// Checks the version pairs the specification executor runs: every pair Stage renders, plus ESM v34, whose only addition
    /// is <c language="text">then invokes</c> expectations (compared).
    /// </summary>
    /// <param name="model">The executable model.</param>
    /// <returns>Whether the executor runs the version pair.</returns>
    public static bool SupportsRun(ExecutableSemanticModel model) =>
        CratisEsmSupport.Descriptor.Classify(model.LanguageVersion, model.SemanticVersion) is EsmVersionStanding.Audited or EsmVersionStanding.NewerWithinRange ||
        (model.LanguageVersion == LanguageVersion.V33 && model.SemanticVersion == SemanticVersion.V33) ||
        (model.LanguageVersion == LanguageVersion.V34 && model.SemanticVersion == SemanticVersion.V34);

    /// <summary>
    /// Finds later-version constructs that block live model admission.
    /// </summary>
    /// <param name="application">The application.</param>
    /// <returns>The precise capability refusals.</returns>
    public static IEnumerable<SemanticAdmissionFeature> ModelFeatures(SemanticApplication application)
    {
        var slices = SemanticVersionFeatures.Slices(application).ToArray();
        var events = slices.SelectMany(slice => slice.Events).ToDictionary(@event => @event.Id);

        return SemanticVersionFeatures.InApplication(application).Except(SemanticVersionFeatures.RoutesInApplication(application))
            .Except(slices.SelectMany(slice => slice.Commands).SelectMany(SemanticVersionFeatures.DescribedHandlersInCommand))
            .Concat(slices.SelectMany(slice => LiveSliceFeatures(slice).Concat(SemanticVersionFeatures.EventReferences(slice, events))))
            .Distinct().Select(Feature).Concat(AnonymousFeatures(application)).Concat(GraphFeatures(application));
    }

    /// <summary>
    /// Finds later-version constructs that block live model admission, leaving out the translation constructs the live
    /// runtime runs around (see <see cref="RunsAround"/>).
    /// </summary>
    /// <param name="application">The application.</param>
    /// <returns>The precise capability refusals.</returns>
    public static IEnumerable<SemanticAdmissionFeature> BlockingModelFeatures(SemanticApplication application) =>
        ModelFeatures(application).Where(feature => RunsAround(application, feature) is null);

    /// <summary>
    /// Says how the live runtime runs a public-event or translation construct without an external integration: public events
    /// are appended to the application's own outbox, and foreign events arrive through seeds or its inbox.
    /// </summary>
    /// <param name="application">The application the feature belongs to.</param>
    /// <param name="feature">The feature found by <see cref="ModelFeatures(SemanticApplication)"/>.</param>
    /// <returns>The note describing what is and is not executed, or null when the live runtime does not run the construct at all.</returns>
public static string? RunsAround(SemanticApplication application, SemanticAdmissionFeature feature) =>
        PublicEventNote(feature) ?? ForeignEventNote(feature) ?? AutomationNote(application, feature) ?? GraphNote(feature);

    static string? PublicEventNote(SemanticAdmissionFeature feature) =>
        HasTag(feature, SemanticVersionFeatures.PublicEvents) && IsKind(feature, "event", "projection")
            ? $"{SemanticVersionFeatures.PublicEvents}: Published to the outbox only: the sandbox appends the public event to its own event store's outbox, never to its event log; nothing outside the sandbox reads it unless another application is run against the same kernel. Projection shapes the evaluator cannot fold refuse the affected command instead of appending."
            : null;

    static string? ForeignEventNote(SemanticAdmissionFeature feature) =>
        HasTag(feature, SemanticVersionFeatures.ForeignEvents) && IsKind(feature, "event", "capture")
            ? $"{SemanticVersionFeatures.ForeignEvents}: Inbound translation runs on what reaches the application's inbox: foreign events arrive through seeds or through the event store's subscription to the origin store's outbox; captures do not poll external sources."
            : null;

    static string? AutomationNote(SemanticApplication application, SemanticAdmissionFeature feature) =>
        HasTag(feature, SemanticVersionFeatures.Automation) && (IsKind(feature, "capture") || IsTranslateSlice(application, feature))
            ? $"{SemanticVersionFeatures.Automation}: Translation integrations are not executed in the sandbox: inbound facts require seeds; outbound event projections publish locally only."
            : null;

    static bool IsTranslateSlice(SemanticApplication application, SemanticAdmissionFeature feature) =>
        IsKind(feature, "slice") && SemanticVersionFeatures.Slices(application)
            .Any(slice => slice.Id.ToString() == feature.Artifact && slice.Kind == SemanticSliceKind.Translate);

    static string? GraphNote(SemanticAdmissionFeature feature) =>
        HasTag(feature, GraphsNotProjected) && IsKind(feature, "graph")
            ? $"{GraphsNotProjected}: Graphs are projected by the kernel's graph sink: the sandbox registers each graph and its projection with the kernel it runs against, embedded or external, and the kernel writes the nodes and edges. The sandbox itself does not read a graph back, and graph specifications are decided by Screenplay's reference graph runner."
            : null;

    static bool HasTag(SemanticAdmissionFeature feature, string tag) => feature.Details.StartsWith($"{tag}:", StringComparison.Ordinal);

    /// <summary>
    /// Finds the graph declarations, graph projections and graph specifications of the application, which the live runtime does not project.
    /// </summary>
    /// <param name="application">The application.</param>
    /// <returns>One note when the application declares any graph construct.</returns>
    public static IEnumerable<SemanticAdmissionFeature> GraphFeatures(SemanticApplication application)
    {
        if (!DeclaresGraphs(application)) yield break;
        yield return new(
            application.Id.ToString(),
            "graph",
            nameof(StageExecutionCapability.Projection),
            $"{GraphsNotProjected}: The application declares {application.Graphs.Length} graph(s) and {application.GraphProjections.Length} graph projection(s), which this runtime registers with the kernel's graph sink and does not read back.");
    }

    static bool DeclaresGraphs(SemanticApplication application) =>
        !application.Graphs.IsDefaultOrEmpty || !application.GraphProjections.IsDefaultOrEmpty || !application.GraphSpecifications.IsDefaultOrEmpty;

    /// <summary>
    /// Finds later-version constructs in a command.
    /// </summary>
    /// <param name="command">The command.</param>
    /// <returns>The precise capability refusals.</returns>
    public static IEnumerable<SemanticAdmissionFeature> CommandFeatures(SemanticCommand command) =>
        SemanticVersionFeatures.InCommand(command)
            .Except(SemanticVersionFeatures.RoutesInCommand(command))
            .Except(SemanticVersionFeatures.DescribedHandlersInCommand(command)).Select(Feature);

    /// <summary>
    /// Finds later-version assertions in a specification.
    /// </summary>
    /// <param name="specification">The specification.</param>
    /// <returns>The precise capability refusals.</returns>
    public static IEnumerable<SemanticAdmissionFeature> SpecificationFeatures(SemanticSpecification specification) =>
        RunSpecificationFeatures(specification).Select(Feature);

    /// <summary>
    /// Returns the first unsupported construct in deterministic precedence order.
    /// </summary>
    /// <param name="plan">The executable plan.</param>
    /// <param name="specification">The specification to check.</param>
    /// <returns>A typed blocker, or null when execution is safe.</returns>
    public static SemanticUnsupportedCapability? Check(SemanticExecutionPlan plan, SemanticSpecification specification) => Check(plan, specification, null);

    /// <summary>Checks the reachable behavior, admitting only renderer-approved rule bodies.</summary>
    /// <param name="plan">The executable plan.</param>
    /// <param name="specification">The specification.</param>
    /// <param name="bodies">The optional compiled bodies.</param>
    /// <returns>The first unsupported capability, or null.</returns>
    public static SemanticUnsupportedCapability? Check(SemanticExecutionPlan plan, SemanticSpecification specification, SemanticBodyExecution? bodies) => CheckSpecification(plan, specification, bodies);

    /// <summary>
    /// Checks a concrete reaction fact before it can reach per-run projection replay.
    /// </summary>
    /// <param name="plan">The executable plan.</param>
    /// <param name="fact">The fact about to be appended.</param>
    /// <returns>The projection refusal, or null when the fact is safe to project.</returns>
    public static SemanticUnsupportedCapability? CheckProjectionFact(SemanticExecutionPlan plan, SemanticSpecificationEvent fact) =>
        CheckProjectionFact(plan, fact, SemanticObserverFilters.Kinds(plan, fact.Route));

    /// <summary>
    /// Checks a concrete fact and its formatted route before projection replay or append.
    /// </summary>
    /// <param name="plan">The executable plan.</param>
    /// <param name="fact">The fact about to be appended.</param>
    /// <param name="route">The formatted fact route, or null for an unrouted fact.</param>
    /// <returns>The projection refusal, or null when the fact is safe to project.</returns>
    public static SemanticUnsupportedCapability? CheckProjectionFact(SemanticExecutionPlan plan, SemanticSpecificationEvent fact, SemanticEventRoute? route) => CheckProjectionFact(plan, fact, route, null);

    /// <summary>Checks a projected fact with the optional compiled body catalog.</summary>
    /// <param name="plan">The executable plan.</param>
    /// <param name="fact">The candidate fact.</param>
    /// <param name="route">The formatted route.</param>
    /// <param name="bodies">The compiled bodies.</param>
    /// <returns>The first projection refusal, or null.</returns>
    public static SemanticUnsupportedCapability? CheckProjectionFact(SemanticExecutionPlan plan, SemanticSpecificationEvent fact, SemanticEventRoute? route, SemanticBodyExecution? bodies) =>
        SemanticImplementationReach.ForFact(plan, fact.EventContract, route, bodies) ??
        SemanticRunTranslationAdmission.Check(plan, new SemanticSpecification(fact.EventContract, "Reaction production", [], [], null, [], [], [], [])
        { WhenAppended = new(fact.EventContract, fact.Values) { EventSource = fact.EventSource, Route = fact.Route } }) ?? SemanticRunProjectionAdmission.CheckFact(plan, fact);

    /// <summary>
    /// Checks completed world rows for reducer-backed state, even when no query was requested.
    /// </summary>
    /// <param name="plan">The executable plan.</param>
    /// <param name="readModels">The read-model identities present in the completed world.</param>
    /// <returns>The projection refusal, or null when no world row requires a reducer body.</returns>
    public static SemanticUnsupportedCapability? CheckProjectionWorld(SemanticExecutionPlan plan, IEnumerable<SemanticId> readModels) =>
        CheckProjectionWorld(plan, readModels, null);

    /// <summary>Checks completed world rows with the optional compiled body catalog.</summary>
    /// <param name="plan">The executable plan.</param>
    /// <param name="readModels">The completed read models.</param>
    /// <param name="bodies">The compiled bodies.</param>
    /// <returns>The first projection refusal, or null.</returns>
    public static SemanticUnsupportedCapability? CheckProjectionWorld(SemanticExecutionPlan plan, IEnumerable<SemanticId> readModels, SemanticBodyExecution? bodies) =>
        SemanticImplementationReach.ForWorld(plan, readModels, bodies);

    internal static IEnumerable<SemanticAdmissionFeature> RuntimeModelFeatures(ExecutableSemanticModel model) =>
        ModelFeatures(model, member => ScopedClockMember(model, member) is null);

    /// <summary>
    /// Finds the clock members (ESM v37) that the live runtime refuses per owning command or notes, instead of refusing the whole model.
    /// Every other unaudited member, including anything security relevant, is never scoped and keeps blocking.
    /// </summary>
    /// <param name="model">The executable model.</param>
    /// <returns>The scoped clock members.</returns>
    internal static IEnumerable<SemanticScopedMember> ScopedClockMembers(ExecutableSemanticModel model)
    {
        var standing = RunStanding(model, new HashSet<SemanticId>());
        if (standing.Version is not (EsmVersionStanding.Audited or EsmVersionStanding.NewerWithinRange)) return [];
        return standing.UnknownMembers.Select(member => ScopedClockMember(model, member)).OfType<SemanticScopedMember>();
    }

    /// <summary>
    /// Refuses a command that uses the command clock, which the live runtime cannot run yet; other commands run.
    /// </summary>
    /// <param name="plan">The executable plan.</param>
    /// <param name="command">The command.</param>
    /// <returns>The refusal, or null when the command does not use the clock.</returns>
    internal static SemanticUnsupportedCapability? CommandRangeRefusal(SemanticExecutionPlan plan, SemanticCommand command) =>
        ScopedClockMembers(plan.Model).FirstOrDefault(scoped => scoped.Command?.Id == command.Id) is { } clock
            ? new(StageExecutionCapability.Command, command.Id.ToString(), $"STAGE-ESM-016: Command '{command.Name}' uses the command clock (ESM member {clock.Member.Member}), which Stage does not run yet (Cratis/Stage#957); other commands run.")
            : null;

    internal static bool HasReadModelProvider(SemanticExecutionPlan plan, SemanticSpecification specification, SemanticId readModel) =>
        plan.Projections.Values.Any(projection => projection.ReadModel == readModel) ||
        specification.GivenReadModels.Any(seed => seed.ReadModel == readModel) ||
        plan.Model.Application.Modules.SelectMany(module => module.Features).SelectMany(AllSlices)
            .SelectMany(slice => slice.Reducers).Any(reducer => reducer.ReadModel == readModel);

    internal static EsmSupportStanding RunStanding(ExecutableSemanticModel model, IReadOnlySet<SemanticId> excluded)
    {
        var version = CratisEsmSupport.Descriptor.Classify(model.LanguageVersion, model.SemanticVersion);
        if (version != EsmVersionStanding.NewerWithinRange && !CratisEsmSupport.Descriptor.ScansMembers(model.LanguageVersion, model.SemanticVersion)) return new(version, []);
        var key = string.Join('/', excluded.Select(id => id.ToString()).Order(StringComparer.Ordinal));
        return _standings.GetOrCreateValue(model).GetOrAdd(
            key,
            static (_, state) =>
            {
                var owners = SemanticVersionFeatures.Slices(state.Model.Application).Select(slice => slice.Id).Append(state.Model.Application.Id).ToHashSet();
                return EsmSupportStanding.Evaluate(CratisEsmSupport.Descriptor, state.Model, EsmRangeSurface.Known, EsmRangeSurface.Scope(owners, state.Excluded));
            },
            (Model: model, Excluded: excluded));
    }

    // Live commands cannot consume pinned generation fixtures. Other bodies refuse only their reachable commands.
    internal static IEnumerable<SemanticAdmissionFeature> RuntimeCommandFeatures(SemanticExecutionPlan plan, SemanticCommand command, Func<string, string?>? ruleName = null, SemanticBodyExecution? bodies = null)
    {
        var features = CommandFeatures(command).Concat(SemanticVersionFeatures.ImplementationGenerationInCommand(command, plan.Model.Application.Concepts, ruleName).Select(Feature));
        var refusal = SemanticImplementationReach.ForCommand(plan, command, bodies);
        if (CommandValidationRefusal(plan, command) is { } validationRefusal)
        {
            features = features.Append(new(command.Id.ToString(), "command", nameof(StageExecutionCapability.Command), validationRefusal));
        }

        return refusal is null ? features : features.Append(new(command.Id.ToString(), "command", refusal.Capability.ToString(), refusal.Details));
    }

    internal static bool RuntimePropertyNamesAreUnique(IEnumerable<SemanticProperty> properties)
    {
        var names = properties.Select(property => property.Name).ToArray();

        return names.Distinct(StringComparer.OrdinalIgnoreCase).Count() == names.Length;
    }

    internal static string? CommandValidationRefusal(SemanticExecutionPlan plan, SemanticCommand command)
    {
        return command.Validations
            .Where(rule => rule.Kind is SemanticValidationRuleKind.AllGreaterThan or SemanticValidationRuleKind.AllGreaterThanOrEqual)
            .Select(rule => AllRuleRefusal(plan, command, rule))
            .FirstOrDefault(refusal => refusal is not null);
    }

    static string? AllRuleRefusal(SemanticExecutionPlan plan, SemanticCommand command, SemanticValidationRule rule)
    {
        var property = command.Properties.FirstOrDefault(property => property.Id == rule.Property);
        var description = $"Command '{command.Name}' validation '{rule.Kind}' on property '{property?.Name ?? rule.Property.ToString()}'";
        if (rule.Operand is not SemanticNumberValue) return $"{description} requires a numeric operand.";

        return IsNumericCollection(plan, property?.Type) ? null : $"{description} requires a collection of numeric primitives or concepts.";
    }

    static bool IsNumericCollection(SemanticExecutionPlan plan, SemanticTypeReference? type)
    {
        var primitive = type?.Kind switch
        {
            SemanticTypeReferenceKind.Concept => plan.Model.Application.Concepts.FirstOrDefault(concept => concept.Id == type.Target)?.Primitive,
            SemanticTypeReferenceKind.Primitive => type.Primitive,
            _ => null
        };
        return type?.IsCollection == true && primitive is SemanticPrimitiveType.WholeNumber or SemanticPrimitiveType.DecimalNumber;
    }

    static IEnumerable<SemanticAdmissionFeature> ModelFeatures(ExecutableSemanticModel model, Func<SemanticUnknownMember, bool> include)
    {
        var standing = RunStanding(model, new HashSet<SemanticId>());
        if (standing.Version is not (EsmVersionStanding.Audited or EsmVersionStanding.NewerWithinRange))
        {
            return [new(model.Application.Id.ToString(), "application", nameof(StageExecutionCapability.PlanIssue), UnsupportedVersion(model))];
        }
        return standing.UnknownMembers.Where(include).Select(member => new SemanticAdmissionFeature(member.Owner.ToString(), member.OwnerKind, nameof(StageExecutionCapability.PlanIssue), UnauditedMember(member)))
            .Concat(ModelFeatures(model.Application));
    }

    static SemanticScopedMember? ScopedClockMember(ExecutableSemanticModel model, SemanticUnknownMember member)
    {
        if (member.Member == "SemanticReadModel.PropertyOrder") return new(member, "readModel", null);
        if (IsApplicationScoped(member)) return IsClockApplicationMember(member) ? new(member, "application", null) : null;
        if (!_clockMemberPrefixes.Any(prefix => member.Member.StartsWith(prefix, StringComparison.Ordinal))) return null;
        if (CommandSegment.Match(member.Path) is { Success: true } command) return ScopedCommandMember(model, member, command);

        return member.Path.Contains(".Specifications[", StringComparison.Ordinal) ? new(member, "specification", null) : null;
    }

    static bool IsApplicationScoped(SemanticUnknownMember member) =>
        member.Member.StartsWith("SemanticApplication.", StringComparison.Ordinal) || member.Member.StartsWith("SemanticTimeZone.", StringComparison.Ordinal);

    static bool IsClockApplicationMember(SemanticUnknownMember member) =>
        string.Equals(member.Member, "SemanticApplication.TimeZone", StringComparison.Ordinal) ||
        string.Equals(member.Member, "SemanticApplication.ContextMembers", StringComparison.Ordinal) ||
        member.Member.StartsWith("SemanticTimeZone.", StringComparison.Ordinal);

    static SemanticScopedMember? ScopedCommandMember(ExecutableSemanticModel model, SemanticUnknownMember member, Match command)
    {
        var owner = SemanticVersionFeatures.Slices(model.Application).FirstOrDefault(slice => slice.Id == member.Owner);
        return owner?.Commands.ElementAtOrDefault(int.Parse(command.Groups["index"].Value, System.Globalization.CultureInfo.InvariantCulture)) is { } found ? new(member, "command", found) : null;
    }

    static SemanticUnsupportedCapability? ModelRefusal(SemanticExecutionPlan plan, SemanticSpecification specification) =>
        RangeRefusal(plan, specification) ?? AnonymousAuthorizationRefusal(plan, specification);

    static SemanticUnsupportedCapability? AnonymousAuthorizationRefusal(SemanticExecutionPlan plan, SemanticSpecification specification)
    {
        var policies = plan.Model.Application.Policies;
        var authorizations = specification.ThenQueries.Select(expected => plan.Queries[expected.Query].Authorization)
            .Prepend(specification.When is { } action ? plan.Commands[action.Command].Authorization : null).ToArray();
        if (authorizations.Any(authorization => SemanticAnonymousAccess.NestsAnonymous(authorization, policies)))
        {
            return new(StageExecutionCapability.PlanIssue, specification.Id.ToString(), "STAGE-ESM-015: Anonymous access must be the whole authorization.");
        }
        return specification.ThenDenied && authorizations.Any(authorization => SemanticAnonymousAccess.IsAnonymous(authorization, policies))
            ? new(StageExecutionCapability.PlanIssue, specification.Id.ToString(), "STAGE-ESM-011: An explicitly anonymous operation cannot satisfy then denied.")
            : null;
    }

    static IEnumerable<SemanticAdmissionFeature> AnonymousFeatures(SemanticApplication application) =>
        SemanticVersionFeatures.Slices(application)
            .SelectMany(slice => slice.Commands.Select(command => (command.Id, command.Name, command.Authorization, Kind: "command"))
                .Concat(slice.Queries.Select(query => (query.Id, query.Name, query.Authorization, Kind: "query"))))
            .Where(operation => SemanticAnonymousAccess.NestsAnonymous(operation.Authorization, application.Policies))
            .Select(operation => new SemanticAdmissionFeature(
                operation.Id.ToString(),
                operation.Kind,
                nameof(StageExecutionCapability.PlanIssue),
                $"STAGE-ESM-015: Authorization of '{operation.Name}' combines or nests require anonymous; anonymous access must be the whole authorization."));

    static SemanticUnsupportedCapability? RangeRefusal(SemanticExecutionPlan plan, SemanticSpecification specification)
    {
        if (!SupportsRun(plan.Model)) return new(StageExecutionCapability.PlanIssue, plan.Model.Application.Id.ToString(), UnsupportedVersion(plan.Model));
        if (plan.IsolatedSlice is null && plan.SliceOf(specification.Id) is { } owner) plan = plan.IsolatedTo(owner);
        var excluded = SemanticVersionFeatures.Slices(plan.Model.Application).Where(slice => plan.IsolatedSlice is not null && slice.Id != plan.IsolatedSlice).Select(slice => slice.Id).ToHashSet();
        return RunStanding(plan.Model, excluded).UnknownMembers.FirstOrDefault() is { } unknown
            ? new(StageExecutionCapability.PlanIssue, unknown.Owner.ToString(), UnauditedMember(unknown))
            : null;
    }

    static string UnsupportedVersion(ExecutableSemanticModel model) =>
        $"STAGE-ESM-016: ESM pair {model.LanguageVersion}/{model.SemanticVersion} is not an audited pair; Stage audits ESM pairs individually (prospective range {CratisEsmSupport.Descriptor.Accepts}; see the ESM feature matrix).";

    static string UnauditedMember(SemanticUnknownMember member) => $"STAGE-ESM-016: Unaudited ESM member {member.Member} at {member.Path}.";

    static SemanticUnsupportedCapability? CommandPlanRefusal(SemanticExecutionPlan plan, SemanticCommand command) =>
        SemanticPlanAdmission.Check(plan, command.Id);

    // Settlement matches reaction and reducer filters against concrete routes before projection reach.
    // Keep non-event triggers, opaque bodies and unsupported context mappings refused in both engines.
    static IEnumerable<SemanticVersionFeature> LiveSliceFeatures(SemanticSlice slice) =>
        SemanticVersionFeatures.InSlice(slice)
            .Except(SemanticVersionFeatures.AutomationSlices(slice))
            .Except(SemanticVersionFeatures.EventReactionsInSlice(slice))
            .Except(SemanticVersionFeatures.ReducerFiltersInSlice(slice))
            .Where(feature => feature.Kind != "reaction" || feature.Code is not ("STAGE-ESM-034" or "STAGE-ESM-035" or "STAGE-ESM-037"));

    static SemanticAdmissionFeature Feature(SemanticVersionFeature feature) => new(feature.Artifact.ToString(), feature.Kind, feature.Capability.ToString(), $"{feature.Code}: {feature.Message}");

    // ESM v5-v10 constructs Stage cannot execute block the run rather than being skipped: a reaction's follow-up work,
    // a generated value or a response would otherwise be dropped and the run could pass.
    // Unsupported automation constructs block every run, since their consequences could follow any command.
    static SemanticUnsupportedCapability? LaterVersionConstruct(SemanticExecutionPlan plan, SemanticSlice[] slices, SemanticSpecification specification)
    {
        var command = specification.When is { } when && plan.Commands.TryGetValue(when.Command, out var found) ? found : null;
        var feature = SemanticVersionFeatures.InApplication(plan.Model.Application)
            .Except(SemanticVersionFeatures.TriggersInApplication(plan.Model.Application))
            .Except(SemanticVersionFeatures.RoutesInApplication(plan.Model.Application))
            .Except(slices.SelectMany(slice => slice.Commands).SelectMany(SemanticVersionFeatures.DescribedHandlersInCommand))
            .Concat(slices.SelectMany(slice => RunSliceFeatures(slice).Except(SemanticVersionFeatures.TranslationFeaturesInSlice(slice))
                .Concat(SemanticVersionFeatures.EventReferences(slice, plan.Events).Where(feature => feature.Code is not (SemanticVersionFeatures.PublicEvents or SemanticVersionFeatures.ForeignEvents)))))
            .Concat(RunSpecificationFeatures(specification))
            .Concat(command is null ? [] : SemanticVersionFeatures.InCommand(command)
                .Except(SemanticVersionFeatures.RoutesInCommand(command))
                .Except(SemanticVersionFeatures.DescribedHandlersInCommand(command)))
            .FirstOrDefault();
        return feature is null ? SemanticRunReactionAdmission.CheckCommands(plan, specification) : new(feature.Capability, feature.Artifact.ToString(), $"{feature.Code}: {feature.Message}");
    }

    static IEnumerable<SemanticVersionFeature> RunSliceFeatures(SemanticSlice slice) =>
        LiveSliceFeatures(slice).Except(SemanticVersionFeatures.NonEventTriggersInSlice(slice));

    // Rendering keeps its own absence and append/redelivery clock refusals. The independent executor can
    // compare keyed instances and resolve a validated authored occurrence without a generated reactor scenario.
    static IEnumerable<SemanticVersionFeature> RunSpecificationFeatures(SemanticSpecification specification) =>
        SemanticVersionFeatures.InSpecification(specification)
            .Except(SemanticVersionFeatures.RoutesInSpecification(specification))
            .Except(SemanticVersionFeatures.RedeliveryInSpecification(specification))
            .Where(feature => feature.Code != SemanticVersionFeatures.Absence &&
                !(feature.Code == SemanticVersionFeatures.Automation && (SemanticSpecificationClock.TryGetOccurrence(specification, out _) ||
                    SemanticSpecificationClock.TryGetAutomaticAction(specification, out _, out _))));

    static IEnumerable<SemanticSlice> AllSlices(SemanticFeature feature) =>
        feature.Slices.Concat(feature.Features.SelectMany(AllSlices));

    static bool SupportedRule(SemanticValidationRule rule, SemanticBodyExecution? bodies) =>
        SupportedRule(rule.Kind) || (rule.Kind is SemanticValidationRuleKind.RulePredicate or SemanticValidationRuleKind.CodeValidation && bodies?.IsAdmitted(rule.RequirementId) == true);

    static bool SupportedRule(SemanticValidationRuleKind kind) => kind is
        SemanticValidationRuleKind.NotEmpty or SemanticValidationRuleKind.Maximum or SemanticValidationRuleKind.Minimum or
        SemanticValidationRuleKind.Equal or SemanticValidationRuleKind.NotEqual or SemanticValidationRuleKind.GreaterThan or
        SemanticValidationRuleKind.GreaterThanOrEqual or SemanticValidationRuleKind.LessThan or
        SemanticValidationRuleKind.LessThanOrEqual or SemanticValidationRuleKind.Length or SemanticValidationRuleKind.Matches or
        SemanticValidationRuleKind.Checksum or SemanticValidationRuleKind.Contains or SemanticValidationRuleKind.NotContains or
        SemanticValidationRuleKind.StartsWith or SemanticValidationRuleKind.EndsWith or SemanticValidationRuleKind.DecimalPlaces or
        SemanticValidationRuleKind.AllGreaterThan or SemanticValidationRuleKind.AllGreaterThanOrEqual;

    static string? EventValueRefusal(SemanticExecutionPlan plan, SemanticEventContract eventContract, IEnumerable<SemanticPropertyValue>? values = null)
    {
        var context = new SemanticApplicationContext(plan);
        var payloadRefusal = eventContract.Properties.Select(property => SemanticValueShapeAdmission.PayloadRefusal(context, property.Type)).FirstOrDefault(refusal => refusal is not null);
        if (payloadRefusal is not null) return payloadRefusal;

        var properties = eventContract.Properties.ToLookup(property => property.Id);
        return (values ?? []).Select(value => EventValueRefusal(context, eventContract, properties, value)).FirstOrDefault(refusal => refusal is not null);
    }

    static string? EventValueRefusal(SemanticApplicationContext context, SemanticEventContract eventContract, ILookup<SemanticId, SemanticProperty> properties, SemanticPropertyValue value)
    {
        if (properties[value.TargetProperty].ToArray() is not [var property])
        {
            return $"Event '{eventContract.Name}' payload property '{value.TargetProperty}' must identify exactly one declared property.";
        }

        return EventValueMatches(context, property.Type, value.Value) ? null : "An event payload value does not match its declared structure.";
    }

    static bool EventValueMatches(SemanticApplicationContext context, SemanticTypeReference type, SemanticValue value)
    {
        if (value is SemanticNullValue) return type.IsOptional;
        if (type.IsCollection) return value is SemanticArrayValue array && array.Values.All(member => EventValueMatches(context, type with { IsCollection = false, IsOptional = false }, member));
        if (type.Kind != SemanticTypeReferenceKind.CompositeType) return value is not SemanticArrayValue and not SemanticCompositeValue;
        return value is SemanticCompositeValue composite && composite.Properties.All(member =>
            context.Types[type.Target].Properties.FirstOrDefault(property => property.Id == member.TargetProperty) is { } property && EventValueMatches(context, property.Type, member.Value));
    }

    static bool IsKind(SemanticAdmissionFeature feature, params string[] kinds) =>
        kinds.Any(kind => string.Equals(feature.Kind, kind, StringComparison.Ordinal));
}
