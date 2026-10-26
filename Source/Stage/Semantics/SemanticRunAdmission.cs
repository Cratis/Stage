// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Contracts.Specifications.Semantic;
using Cratis.Stage.Rendering.Cratis.Semantics;
using Cratis.Stage.Rendering.Cratis.Semantics.Projections;
using Cratis.Stage.Specifications.Commands;

namespace Cratis.Stage.Semantics;

/// <summary>
/// Checks the entire reachable behavior before allowing a specification to execute.
/// </summary>
public static class SemanticRunAdmission
{
    const string UnsupportedVersion = "STAGE-ESM-016: The model's language/semantic version is not one Stage has audited.";

    /// <summary>
    /// Checks the audited version pair before finding constructs that block live model admission.
    /// </summary>
    /// <param name="model">The executable model.</param>
    /// <returns>The precise capability refusals.</returns>
    public static IEnumerable<SemanticAdmissionFeature> ModelFeatures(ExecutableSemanticModel model) =>
        SemanticVersionFeatures.Supports(model)
            ? ModelFeatures(model.Application)
            : [new(model.Application.Id.ToString(), "application", nameof(StageExecutionCapability.PlanIssue), UnsupportedVersion)];

    /// <summary>
    /// Checks the version pairs the specification executor runs: every pair Stage renders, plus ESM v33 and v34, whose only additions
    /// are captures keyed by event source (refused per capture) and <c language="text">then invokes</c> expectations (compared).
    /// </summary>
    /// <param name="model">The executable model.</param>
    /// <returns>Whether the executor runs the version pair.</returns>
    public static bool SupportsRun(ExecutableSemanticModel model) =>
        SemanticVersionFeatures.Supports(model) ||
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
            .Distinct().Select(Feature);
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
    public static string? RunsAround(SemanticApplication application, SemanticAdmissionFeature feature)
    {
        if (feature.Details.StartsWith($"{SemanticVersionFeatures.PublicEvents}:", StringComparison.Ordinal) && IsKind(feature, "event", "projection"))
        {
            return $"{SemanticVersionFeatures.PublicEvents}: Published to the outbox only: the sandbox appends the public event to its own event store's outbox, never to its event log; nothing outside the sandbox reads it unless another application is run against the same kernel. Projection shapes the evaluator cannot fold refuse the affected command instead of appending.";
        }

        if (feature.Details.StartsWith($"{SemanticVersionFeatures.ForeignEvents}:", StringComparison.Ordinal) && IsKind(feature, "event", "capture"))
        {
            return $"{SemanticVersionFeatures.ForeignEvents}: Inbound translation runs on what reaches the application's inbox: foreign events arrive through seeds or through the event store's subscription to the origin store's outbox; captures do not poll external sources.";
        }

        if (feature.Details.StartsWith($"{SemanticVersionFeatures.Automation}:", StringComparison.Ordinal) &&
            (IsKind(feature, "capture") || (IsKind(feature, "slice") && SemanticVersionFeatures.Slices(application)
                .Any(slice => slice.Id.ToString() == feature.Artifact && slice.Kind == SemanticSliceKind.Translate))))
        {
            return $"{SemanticVersionFeatures.Automation}: Translation integrations are not executed in the sandbox: inbound facts require seeds; outbound event projections publish locally only.";
        }

        return null;
    }

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
    public static SemanticUnsupportedCapability? Check(SemanticExecutionPlan plan, SemanticSpecification specification)
    {
        static SemanticUnsupportedCapability Block(StageExecutionCapability capability, SemanticId id, string details) => new(capability, id.ToString(), details);
        if (!SupportsRun(plan.Model)) return Block(StageExecutionCapability.PlanIssue, plan.Model.Application.Id, UnsupportedVersion);

        // A specification runs only its own slice (Screenplay decision 0115): admit what that slice runs, not other slices' reactions or captures.
        if (plan.IsolatedSlice is null && plan.SliceOf(specification.Id) is { } owner) plan = plan.IsolatedTo(owner);
        if (plan.RunningCaptures.FirstOrDefault(capture => capture.EventsSource is { KeyIsEventSource: true }) is { } keyedBySource)
        {
            return Block(StageExecutionCapability.ExternalEffect, keyedBySource.Id, $"STAGE-ESM-016: Capture '{keyedBySource.Name}' keys its records by event source (ESM v33), which Stage does not run yet.");
        }
        var slices = plan.Model.Application.Modules.SelectMany(module => module.Features).SelectMany(AllSlices).ToArray();
        if (LaterVersionConstruct(plan, slices, specification) is { } later) return later;
        if (SemanticRunTranslationAdmission.Check(plan, specification) is { } translation) return translation;

        // Runtime contracts keep authored property names, not the renderer's PascalCase members.
        // Case-insensitive duplicates still make JSON binding and Chronicle payloads ambiguous.
        foreach (var slice in slices)
        {
            var collidingEvent = slice.Events.FirstOrDefault(@event => !RuntimePropertyNamesAreUnique(@event.Properties));
            if (collidingEvent is not null)
            {
                var referenced = slices.SelectMany(candidate => candidate.Projections)
                    .Any(projection => ProjectionReferencedEventNamesAreUnique.Contracts(projection).Contains(collidingEvent.Id));
                return Block(
                    referenced ? StageExecutionCapability.Projection : StageExecutionCapability.Command,
                    collidingEvent.Id,
                    "Event property names are not unique under case-insensitive runtime JSON binding.");
            }
            var collidingCommand = slice.Commands.FirstOrDefault(command => !RuntimePropertyNamesAreUnique(command.Properties));
            if (collidingCommand is not null) return Block(StageExecutionCapability.Command, collidingCommand.Id, "Command property names are not unique under case-insensitive runtime JSON binding.");
            var collidingReadModel = slice.ReadModels.FirstOrDefault(readModel => !RuntimePropertyNamesAreUnique(readModel.Properties));
            if (collidingReadModel is not null) return Block(StageExecutionCapability.Projection, collidingReadModel.Id, "Read-model property names are not unique under case-insensitive runtime JSON binding.");
        }
        var collidingType = plan.Model.Application.Types.FirstOrDefault(type => !RuntimePropertyNamesAreUnique(type.Properties));
        if (collidingType is not null) return Block(StageExecutionCapability.Command, collidingType.Id, "Type property names are not unique under case-insensitive runtime JSON binding.");
        if (SemanticImplementationReach.ForSpecification(plan, specification) is { } implementation) return implementation;
        if (specification.GivenCaller is { Authenticated: false } caller && (!caller.Roles.IsEmpty || !caller.Claims.IsEmpty))
        {
            return Block(StageExecutionCapability.Authorization, specification.Id, "An unauthenticated caller cannot carry roles or claims; Arc supplies an empty guest principal.");
        }
        if (specification.GivenCaller?.Claims.Any(claim => string.Equals(claim.Type, ClaimTypes.Role, StringComparison.OrdinalIgnoreCase)) == true)
        {
            return Block(StageExecutionCapability.Authorization, specification.Id, "Role-URI claim types cannot be used as claims; roles and claims are separate in Screenplay.");
        }
        if (specification.WhenAppended is { } appended)
        {
            if (!plan.Events.TryGetValue(appended.EventContract, out var appendedContract)) return Block(StageExecutionCapability.PlanIssue, appended.EventContract, "The appended event is not in the plan.");
            if (EventValueRefusal(plan, appendedContract, appended.Values) is { } appendedRefusal) return Block(StageExecutionCapability.Occurrence, appended.EventContract, appendedRefusal);
        }
        foreach (var given in specification.GivenEvents)
        {
            if (!plan.Events.TryGetValue(given.EventContract, out var givenContract)) return Block(StageExecutionCapability.PlanIssue, given.EventContract, "The Given event is not in the plan.");
            if (EventValueRefusal(plan, givenContract, given.Values) is { } givenRefusal) return Block(StageExecutionCapability.Command, given.EventContract, givenRefusal);
        }
        if (specification.WhenAppended is not null) return SemanticRunReactionAdmission.CheckProjections(plan, specification, specification.GivenEvents.Select(given => given.EventContract).Append(specification.WhenAppended.EventContract));
        if (specification.When is not { } when)
        {
            return SemanticRunReactionAdmission.CheckProjections(plan, specification, specification.GivenEvents.Select(given => given.EventContract));
        }
        if (!plan.Commands.TryGetValue(when.Command, out var command)) return Block(StageExecutionCapability.Command, when.Command, "The command is not in the plan.");
        if (CommandPlanRefusal(plan, command) is { } commandRefusal) return commandRefusal;
        if (CommandValidationRefusal(plan, command) is { } validationRefusal)
        {
            return Block(StageExecutionCapability.Command, command.Id, validationRefusal);
        }
        var valueContext = new SemanticApplicationContext(plan);
        var valueRefusal = command.Properties.Select(property => SemanticValueShapeAdmission.Refusal(valueContext, property.Type)).FirstOrDefault(refusal => refusal is not null);
        if (valueRefusal is not null)
        {
            return Block(StageExecutionCapability.Command, command.Id, valueRefusal);
        }

        // A described handler determines its own productions. Only history can be projected before its body is reached.
        if (command.Handler is not null)
        {
            if (command.Validations.Any(rule => !SupportedRule(rule.Kind)))
            {
                return Block(StageExecutionCapability.Command, command.Id, "A command validation rule is not admitted.");
            }

            return SemanticRunReactionAdmission.CheckProjections(plan, specification, specification.GivenEvents.Select(given => given.EventContract));
        }

        // Projections never run for expected rejection/denial, whether the command rejects or reports an acceptance mismatch.
        var produced = specification.ThenErrors.IsEmpty && !specification.ThenDenied
            ? SemanticProductionConditions.Taken(command, when, plan.Model.Application).Select(produce => produce.EventContract) : [];
        var reachableEvents = specification.GivenEvents.Select(given => given.EventContract).Concat(produced).ToHashSet();
        if (SemanticRunReactionAdmission.CheckProjections(plan, specification, reachableEvents) is { } projectionBlock) return projectionBlock;

        foreach (var production in command.Produces)
        {
            if (!plan.Events.TryGetValue(production.EventContract, out var eventContract)) return Block(StageExecutionCapability.Command, command.Id, "The produced event is not in the plan.");
            if (EventValueRefusal(plan, eventContract) is { } eventRefusal) return Block(StageExecutionCapability.Command, command.Id, eventRefusal);
        }

        if (command.Validations.Any(rule => !SupportedRule(rule.Kind)))
        {
            return Block(StageExecutionCapability.Command, command.Id, "A command validation rule is not admitted.");
        }

        if (command.Produces.Any(produced => produced.Condition is not null || produced.Mappings.Any(mapping => mapping.Source is not (SemanticValueExpression or SemanticReadPathExpression or SemanticResolvedExpression { Root: SemanticExpressionRootKind.Command, Source: SemanticExpressionSourceKind.Property }) &&
                !(mapping.Source is SemanticEventContextExpression { Value: SemanticEventContextValueKind.Occurred } &&
                    (SemanticSpecificationClock.TryGetOccurrence(specification, out _) || !specification.ThenErrors.IsEmpty || specification.ThenDenied)))))
        {
            return Block(StageExecutionCapability.Command, command.Id, "Expression conditions or non-command mappings are not admitted.");
        }

        if (command.Produces.Any(produced => produced.Destination is not null and not (SemanticValueExpression or SemanticResolvedExpression { Root: SemanticExpressionRootKind.Command })))
        {
            return Block(StageExecutionCapability.Command, command.Id, "The destination expression is not admitted.");
        }

        // A routed command can reject its route before reaching identity allocation; defer that decision to execution.
        if (specification.ThenErrors.IsEmpty && command.Route is null && when.EventSource is null && command.Destination?.Value is null && SemanticProductionConditions.Taken(command, when, plan.Model.Application).Any(produced => produced.Destination is null))
        {
            return Block(StageExecutionCapability.IdentityAllocation, command.Id, "An accepted command requires an explicit destination.");
        }
        return null;
    }

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
    public static SemanticUnsupportedCapability? CheckProjectionFact(SemanticExecutionPlan plan, SemanticSpecificationEvent fact, SemanticEventRoute? route) =>
        SemanticImplementationReach.ForFact(plan, fact.EventContract, route) ??
        SemanticRunTranslationAdmission.Check(plan, new SemanticSpecification(fact.EventContract, "Reaction production", [], [], null, [], [], [], [])
        { WhenAppended = new(fact.EventContract, fact.Values) { EventSource = fact.EventSource, Route = fact.Route } }) ?? SemanticRunProjectionAdmission.CheckFact(plan, fact);

    /// <summary>
    /// Checks completed world rows for reducer-backed state, even when no query was requested.
    /// </summary>
    /// <param name="plan">The executable plan.</param>
    /// <param name="readModels">The read-model identities present in the completed world.</param>
    /// <returns>The projection refusal, or null when no world row requires a reducer body.</returns>
    public static SemanticUnsupportedCapability? CheckProjectionWorld(SemanticExecutionPlan plan, IEnumerable<SemanticId> readModels) =>
        SemanticImplementationReach.ForWorld(plan, readModels);

    internal static bool HasReadModelProvider(SemanticExecutionPlan plan, SemanticSpecification specification, SemanticId readModel) =>
        plan.Projections.Values.Any(projection => projection.ReadModel == readModel) ||
        specification.GivenReadModels.Any(seed => seed.ReadModel == readModel) ||
        plan.Model.Application.Modules.SelectMany(module => module.Features).SelectMany(feature => feature.Slices)
            .SelectMany(slice => slice.Reducers).Any(reducer => reducer.ReadModel == readModel);

    // Live commands cannot consume pinned generation fixtures. Other bodies refuse only their reachable commands.
    internal static IEnumerable<SemanticAdmissionFeature> RuntimeCommandFeatures(SemanticExecutionPlan plan, SemanticCommand command, Func<string, string?>? ruleName = null)
    {
        var features = CommandFeatures(command).Concat(SemanticVersionFeatures.ImplementationGenerationInCommand(command, plan.Model.Application.Concepts, ruleName).Select(Feature));
        var refusal = SemanticImplementationReach.ForCommand(plan, command);
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
        foreach (var rule in command.Validations.Where(rule => rule.Kind is SemanticValidationRuleKind.AllGreaterThan or SemanticValidationRuleKind.AllGreaterThanOrEqual))
        {
            var property = command.Properties.FirstOrDefault(property => property.Id == rule.Property);
            var description = $"Command '{command.Name}' validation '{rule.Kind}' on property '{property?.Name ?? rule.Property.ToString()}'";
            if (rule.Operand is not SemanticNumberValue)
            {
                return $"{description} requires a numeric operand.";
            }
            var type = property?.Type;
            var primitive = type?.Kind switch
            {
                SemanticTypeReferenceKind.Concept => plan.Model.Application.Concepts.FirstOrDefault(concept => concept.Id == type.Target)?.Primitive,
                SemanticTypeReferenceKind.Primitive => type.Primitive,
                _ => null
            };
            if (type?.IsCollection != true || primitive is not (SemanticPrimitiveType.WholeNumber or SemanticPrimitiveType.DecimalNumber))
            {
                return $"{description} requires a collection of numeric primitives or concepts.";
            }
        }

        return null;
    }

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
        foreach (var property in eventContract.Properties)
        {
            if (SemanticValueShapeAdmission.PayloadRefusal(context, property.Type) is { } refusal) return refusal;
        }
        var properties = eventContract.Properties.ToLookup(property => property.Id);
        foreach (var value in values ?? [])
        {
            if (properties[value.TargetProperty].ToArray() is not [var property])
            {
                return $"Event '{eventContract.Name}' payload property '{value.TargetProperty}' must identify exactly one declared property.";
            }
            if (!EventValueMatches(context, property.Type, value.Value))
            {
                return "An event payload value does not match its declared structure.";
            }
        }
        return null;
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
