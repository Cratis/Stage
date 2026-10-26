// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Stage.Contracts.Specifications.Semantic;
using Cratis.Stage.Rendering.Cratis.Naming;
using Cratis.Stage.Rendering.Cratis.Semantics.Projections;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

/// <summary>
/// One ESM v5–v10 construct a Stage execution or rendering path refuses.
/// </summary>
/// <param name="Code">The typed STAGE-ESM diagnostic code.</param>
/// <param name="Capability">The execution capability the construct needs.</param>
/// <param name="Artifact">The declaration that uses the construct.</param>
/// <param name="Kind">The kind of declaration: trigger, slice, reaction, capture, command or specification.</param>
/// <param name="Message">What the construct is and why it is refused.</param>
internal sealed record SemanticVersionFeature(string Code, StageExecutionCapability Capability, SemanticId Artifact, string Kind, string Message);

/// <summary>
/// Finds the ESM v5–v10 constructs that renderer admission, the specification executor and the Host runtime refuse.
/// </summary>
/// <remarks>
/// One detection serves all three (the Host and the specification executor compile this file). Routes have separate
/// detectors so the Host can execute them while renderer and specification admission continue to refuse them.
/// </remarks>
internal static class SemanticVersionFeatures
{
    /// <summary>
    /// The code for ESM v6 triggers, reactions, captures, automation and translate slices and their specification forms.
    /// </summary>
    internal const string Automation = "STAGE-ESM-024";

    /// <summary>
    /// The code for ESM v5 keyed read-model absence expectations.
    /// </summary>
    internal const string Absence = "STAGE-ESM-027";

    /// <summary>
    /// The code for ESM v7 generated command values and generation fixtures.
    /// </summary>
    internal const string Generated = "STAGE-ESM-028";

    /// <summary>
    /// The code for ESM v7 command responses and return expectations.
    /// </summary>
    internal const string Responses = "STAGE-ESM-029";

    /// <summary>
    /// The code for generated-value or response combinations that cannot be preserved by the target.
    /// </summary>
    internal const string CommandResults = "STAGE-ESM-038";

    /// <summary>
    /// The code for ESM v8 event sources, streams and routes, shared with renderer admission and the semantic surface ledger.
    /// </summary>
    internal const string Routes = "STAGE-ESM-030";

    /// <summary>
    /// The code for ESM v9 public-event publication.
    /// </summary>
    internal const string PublicEvents = "STAGE-ESM-032";

    /// <summary>
    /// The code for ESM v9 foreign events and events-source captures.
    /// </summary>
    internal const string ForeignEvents = "STAGE-ESM-033";

    /// <summary>
    /// The code for ESM v10 reaction execution identity.
    /// </summary>
    internal const string ReactionIdentity = "STAGE-ESM-034";

    /// <summary>
    /// The code for ESM v10 reaction and reducer observer filters.
    /// </summary>
    internal const string ObserverFilters = "STAGE-ESM-035";

    /// <summary>
    /// The code for ESM v10 per-production route overrides that Stage does not admit yet.
    /// </summary>
    internal const string ProductionRoutes = "STAGE-ESM-036";

    /// <summary>
    /// The code for a reaction trigger that both produces events and invokes commands.
    /// </summary>
    internal const string MixedReactionEffects = "STAGE-ESM-037";

    /// <summary>
    /// The code for compliance on identity uses or enumeration concepts.
    /// </summary>
    internal const string ProtectedIdentity = "STAGE-ESM-040";

    /// <summary>
    /// The code for checksum validation and other validation rules the legacy renderer cannot preserve.
    /// </summary>
    internal const string UnsupportedValidation = "STAGE-ESM-042";

    /// <summary>
    /// The code for atomic reaction batch shapes whose production boundary the Cratis renderer cannot preserve.
    /// </summary>
    internal const string AtomicReactionBatches = "STAGE-ESM-043";

    /// <summary>
    /// The code for the protected command reads a target cannot preserve, and for the legacy syntax path that has no protected reads at all.
    /// </summary>
    internal const string ProtectedCommandReads = "STAGE-ESM-044";

    /// <summary>
    /// The code for protected-read requirements whose non-Error severity semantics are not defined yet.
    /// </summary>
    internal const string ProtectedReadRequirementSeverity = "STAGE-ESM-045";

    /// <summary>
    /// The code for chronological command date comparisons a rendering target cannot preserve.
    /// </summary>
    internal const string DateOrdering = "STAGE-ESM-046";

    /// <summary>
    /// The code for single-instance queries by composite key parts, which the renderer cannot look up until Chronicle#4265.
    /// </summary>
    internal const string KeyPartsLookup = "STAGE-ESM-047";

    /// <summary>
    /// The code for read models with several key parts, or parts projection keys, in a shape the renderer cannot preserve.
    /// </summary>
    internal const string CompositeReadModelKeys = "STAGE-ESM-048";

    /// <summary>
    /// The code for Uuid comparisons, which the legacy syntax engine cannot keep the meaning of.
    /// </summary>
    internal const string UuidComparisons = "STAGE-ESM-049";

    /// <summary>
    /// The code for projection add or subtract by a product (ESM v29), which Chronicle's projection engine cannot compute.
    /// </summary>
    internal const string ProjectionProducts = "STAGE-ESM-050";

    /// <summary>
    /// The code for pending command handlers, which no Stage engine implements yet.
    /// </summary>
    internal const string PendingCommandHandler = "STAGE-IMPL-002";

    const string ArcDefaultStreamId = "Default";

    /// <summary>
    /// Checks the language and semantic version pair Stage has audited.
    /// </summary>
    /// <param name="model">The executable model.</param>
    /// <returns>Whether Stage supports the version pair.</returns>
    public static bool Supports(ExecutableSemanticModel model) =>
        EsmSchemaV30Support.Supports(model.LanguageVersion, model.SemanticVersion) ||
        (model.LanguageVersion == LanguageVersion.V32 && model.SemanticVersion == SemanticVersion.V32) ||
        (model.LanguageVersion == LanguageVersion.V33 && model.SemanticVersion == SemanticVersion.V33);

    /// <summary>
    /// Gets every slice in the application in model order, including slices of nested features.
    /// </summary>
    /// <param name="application">The semantic application.</param>
    /// <returns>The slices.</returns>
    public static IEnumerable<SemanticSlice> Slices(SemanticApplication application) =>
        application.Modules.SelectMany(module => module.Features).SelectMany(Slices);

    /// <summary>
    /// Finds application-level constructs that belong to no slice.
    /// </summary>
    /// <param name="application">The semantic application.</param>
    /// <returns>The refused constructs.</returns>
    public static IEnumerable<SemanticVersionFeature> InApplication(SemanticApplication application) =>
        TriggersInApplication(application)
        .Concat(Slices(application).SelectMany(slice => slice.Commands).SelectMany(DescribedHandlersInCommand))
        .Concat(RoutesInApplication(application))
        .Concat(application.Concepts.SelectMany(concept => UnknownValidationRules(concept.Validations, concept.Id, "concept", concept.Name)))
        .Concat(Slices(application).SelectMany(slice => slice.Commands).SelectMany(command => UnknownValidationRules(command.Validations, command.Id, "command", command.Name)))
        .Concat(Slices(application).SelectMany(slice => slice.Commands).SelectMany(command => UnsupportedGenerated(command, application.Concepts)));

    /// <summary>
    /// Finds application triggers that targets must explicitly admit.
    /// </summary>
    /// <param name="application">The application.</param>
    /// <returns>The application trigger refusals in declaration order.</returns>
    public static IEnumerable<SemanticVersionFeature> TriggersInApplication(SemanticApplication application) =>
        (application.Triggers.IsDefault ? [] : application.Triggers).Select(trigger => new SemanticVersionFeature(
            Automation, StageExecutionCapability.Reaction, trigger.Id, "trigger", $"Application trigger '{trigger.Name}' is an ESM v6 automation construct, which Stage does not support yet."));

    /// <summary>
    /// Finds atomic reaction batch shapes the renderer cannot preserve. One returned collection is one append batch.
    /// </summary>
    /// <param name="application">The semantic application.</param>
    /// <param name="slice">The selected slice.</param>
    /// <returns>The refused reaction batch shapes.</returns>
    public static IEnumerable<SemanticVersionFeature> ReactionBatchesInSlice(SemanticApplication application, SemanticSlice slice)
    {
        if (application.ReactionProductionMode is not SemanticReactionProductionMode.Atomic) yield break;

        foreach (var reaction in slice.Reactions.IsDefault ? [] : slice.Reactions)
        {
            foreach (var group in reaction.Triggers.Where(trigger => trigger.Kind == SemanticReactionTriggerKind.Event).GroupBy(trigger => trigger.Source)
                .Where(group => group.Count(trigger => !trigger.Produces.IsDefaultOrEmpty) > 1))
            {
                yield return new(AtomicReactionBatches, StageExecutionCapability.Reaction, reaction.Id, "reaction", $"Atomic reaction '{reaction.Name}' has multiple same-event production triggers for '{group.Key}'; they would share one append batch instead of retaining separate trigger batches.");
            }
            if (reaction.Triggers.SelectMany(trigger => trigger.Produces)
                .Any(produced => produced.Route is not null))
            {
                yield return new(AtomicReactionBatches, StageExecutionCapability.Reaction, reaction.Id, "reaction", $"Atomic reaction '{reaction.Name}' has routed direct productions; routed batches and stream closing are not covered by Screenplay's atomic batch contract yet (Cratis/Screenplay#770).");
            }
        }
    }

    /// <summary>
    /// Finds a described command body that must not be replaced by declarative mappings.
    /// </summary>
    /// <param name="command">The command.</param>
    /// <returns>The pending handler refusal.</returns>
    public static IEnumerable<SemanticVersionFeature> DescribedHandlersInCommand(SemanticCommand command)
    {
        if (command.Handler is { } handler)
        {
            yield return new(PendingCommandHandler, StageExecutionCapability.Command, command.Id, "command", $"Command '{command.Name}' has pending command handler requirement '{handler.RequirementId}'; described command handlers are not implemented by Stage yet.");
        }
    }

    /// <summary>
    /// Finds named event sources and streams the renderer cannot render yet.
    /// </summary>
    /// <param name="application">The semantic application.</param>
    /// <returns>The routed declarations.</returns>
    public static IEnumerable<SemanticVersionFeature> RoutesInApplication(SemanticApplication application) =>
        (application.EventSources.IsDefault ? [] : application.EventSources).Select(source => new SemanticVersionFeature(
            Routes, StageExecutionCapability.IdentityAllocation, source.Id, "eventsource", "Named event sources and streams are not yet supported by the Cratis ESM planner."));

    /// <summary>
    /// Finds the ESM v27 single-instance lookups by key parts of one slice, which the renderer refuses.
    /// Composite-keyed read models and their parts projections are admitted by the state-view admission, which refuses the shapes it cannot render with <see cref="CompositeReadModelKeys"/>.
    /// </summary>
    /// <param name="slice">The slice.</param>
    /// <returns>One refusal per query by key parts, in model order.</returns>
    public static IEnumerable<SemanticVersionFeature> CompositeKeysInSlice(SemanticSlice slice)
    {
        foreach (var query in slice.Queries.Where(query => !query.KeyParts.IsDefaultOrEmpty))
        {
            yield return new(KeyPartsLookup, StageExecutionCapability.Query, query.Id, "query", $"Query '{query.Name}' looks up one instance by key parts (ESM v27); Chronicle cannot look up a read model by a composite key until Chronicle#4265.");
        }
    }

    /// <summary>
    /// Checks whether a read model is identified by several key parts.
    /// </summary>
    /// <param name="readModel">The read model.</param>
    /// <returns>Whether the read model has two or more key parts.</returns>
    public static bool HasCompositeKey(SemanticReadModel readModel) => readModel.Properties.Count(property => property.IsIdentifier) > 1;

    /// <summary>
    /// Finds the ESM v29 projection products of one slice that the renderer cannot lower. Chronicle's Add and Subtract take one event
    /// property, so a product of two event values is rendered as a member the event carries (<see cref="SemanticProjectionProducts"/>);
    /// one that cannot be (a join or every block, optional or non-numeric factors, a total of another number type) is refused with a precise code.
    /// </summary>
    /// <param name="slice">The slice.</param>
    /// <param name="context">The semantic application.</param>
    /// <returns>One refusal per product that does not lower, in model order.</returns>
    public static IEnumerable<SemanticVersionFeature> ProjectionProductsInSlice(SemanticSlice slice, SemanticApplicationContext context) =>
        slice.Projections.Where(projection => projection.Scope is not null && context.ReadModels.ContainsKey(projection.ReadModel)).SelectMany(projection =>
            SemanticProjectionProducts.Walk(projection.Scope!, context.ReadModels[projection.ReadModel].Properties, context)
                .Where(found => found.Rejection is not null)
                .Select(found => new SemanticVersionFeature(
                    ProjectionProducts,
                    StageExecutionCapability.Projection,
                    projection.Id,
                    "projection",
                    $"Projection '{projection.Name}' adds or subtracts a product of two event values (ESM v29) that the Cratis renderer cannot lower: {found.Rejection}.")));

    /// <summary>
    /// Finds the structural automation and public-event constructs of one slice.
    /// </summary>
    /// <param name="slice">The slice.</param>
    /// <returns>The refused constructs.</returns>
    public static IEnumerable<SemanticVersionFeature> InSlice(SemanticSlice slice)
    {
        // Precise reaction constructs precede the generic slice refusal for first-refusal consumers.
        foreach (var feature in ReactionsInSlice(slice))
        {
            yield return feature;
        }

        foreach (var filter in ReducerFiltersInSlice(slice)) yield return filter;

        foreach (var feature in AutomationSlices(slice)) yield return feature;

        if (slice.Kind == SemanticSliceKind.Translate)
        {
            var direction = $" ({slice.Direction?.ToString() ?? "Inbound"})";
            yield return new(Automation, StageExecutionCapability.Reaction, slice.Id, "slice", $"Slice '{slice.Name}' is an ESM v6 {slice.Kind}{direction} slice; Stage renders only StateChange and StateView slices.");
        }

        foreach (var @event in slice.Events)
        {
            if (InEvent(@event) is { } feature) yield return feature;
        }

        foreach (var projection in slice.Projections.Where(projection => projection.Target == SemanticProjectionTargetKind.Event))
        {
            yield return new(PublicEvents, StageExecutionCapability.Projection, projection.Id, "projection", $"Projection '{projection.Name}' publishes an ESM v9 public event, which Stage does not support yet.");
        }

        foreach (var reducer in slice.Reducers.Where(reducer => reducer.Target == SemanticProjectionTargetKind.Event))
        {
            yield return new(PublicEvents, StageExecutionCapability.Projection, reducer.ReadModel, "reducer", $"Reducer '{reducer.Name}' publishes an ESM v9 public event, which Stage does not support yet.");
        }

        foreach (var capture in slice.Captures.IsDefault ? [] : slice.Captures)
        {
            if (capture.EventsSource is not null)
            {
                yield return new(ForeignEvents, StageExecutionCapability.ExternalEffect, capture.Id, "capture", $"Capture '{capture.Name}' consumes ESM v9 source events, which Stage does not support yet.");
            }
            else
            {
                yield return new(Automation, StageExecutionCapability.ExternalEffect, capture.Id, "capture", $"Capture '{capture.Name}' is an ESM v6 translation construct, which Stage does not support yet.");
            }
        }
    }

    /// <summary>
    /// Finds translation refusals that specification execution scopes to reachable behavior.
    /// </summary>
    /// <param name="slice">The slice.</param>
    /// <returns>The translation features, with the same diagnostics as rendering.</returns>
    public static IEnumerable<SemanticVersionFeature> TranslationFeaturesInSlice(SemanticSlice slice) =>
        InSlice(slice).Where(feature => feature.Code == PublicEvents || feature.Code == ForeignEvents ||
            (feature.Code == Automation && (feature.Kind == "capture" || (feature.Kind == "slice" && slice.Kind == SemanticSliceKind.Translate))));

    /// <summary>
    /// Finds every refused reaction construct, with precise reasons before the generic reaction refusal.
    /// </summary>
    /// <param name="slice">The slice.</param>
    /// <returns>The refused reaction constructs, including event reactions not yet admitted for rendering.</returns>
    public static IEnumerable<SemanticVersionFeature> ReactionsInSlice(SemanticSlice slice)
    {
        var reactions = slice.Reactions.IsDefault ? [] : slice.Reactions;
        foreach (var feature in ReactionIdentitiesInSlice(slice)) yield return feature;

        foreach (var feature in ReactionFiltersInSlice(slice)) yield return feature;

        foreach (var feature in reactions.SelectMany(MixedEffectsInReaction)) yield return feature;

        foreach (var feature in reactions.SelectMany(UnsupportedReactionContexts)) yield return feature;

        foreach (var reaction in reactions.Where(reaction => reaction.Triggers.SelectMany(trigger => trigger.Invokes)
            .SelectMany(invocation => invocation.OnRefused).SelectMany(branch => branch.Produces)
            .Any(produced => produced.Condition is not null || produced.When is not null || produced.Route is not null)))
        {
            yield return new("STAGE-ESM-039", StageExecutionCapability.Reaction, reaction.Id, "reaction", $"Reaction '{reaction.Name}' has a refusal production with a condition or route override, which Stage cannot preserve.");
        }

        foreach (var reaction in reactions)
        {
            foreach (var trigger in reaction.Triggers)
            {
                if (trigger.RequirementId is not null)
                {
                    yield return new(Automation, StageExecutionCapability.Reaction, reaction.Id, "reaction", $"Reaction '{reaction.Name}' has an opaque body for trigger kind '{trigger.Kind}', which Stage does not support yet.");
                }

                foreach (var feature in NonEventTrigger(reaction, trigger)) yield return feature;
            }
        }

        foreach (var feature in EventReactionsInSlice(slice)) yield return feature;
    }

    /// <summary>
    /// Finds non-event triggers that targets must explicitly admit, without admitting opaque bodies.
    /// </summary>
    /// <param name="slice">The slice.</param>
    /// <returns>The non-event trigger refusals in declaration order.</returns>
    public static IEnumerable<SemanticVersionFeature> NonEventTriggersInSlice(SemanticSlice slice) =>
        (slice.Reactions.IsDefault ? [] : slice.Reactions).SelectMany(reaction => reaction.Triggers.SelectMany(trigger => NonEventTrigger(reaction, trigger)));

    /// <summary>
    /// Finds reaction observer filters that targets must explicitly admit.
    /// </summary>
    /// <param name="slice">The slice.</param>
    /// <returns>The declared observer filters.</returns>
    public static IEnumerable<SemanticVersionFeature> ReactionFiltersInSlice(SemanticSlice slice) =>
        (slice.Reactions.IsDefault ? [] : slice.Reactions).Where(reaction => reaction.From is not null)
            .Select(reaction => new SemanticVersionFeature(ObserverFilters, StageExecutionCapability.Reaction, reaction.Id, "reaction", $"Reaction '{reaction.Name}' declares an ESM v10 observer filter."));

    /// <summary>
    /// Finds reaction execution identities that targets must explicitly admit.
    /// </summary>
    /// <param name="slice">The slice.</param>
    /// <returns>The declared execution identities.</returns>
    public static IEnumerable<SemanticVersionFeature> ReactionIdentitiesInSlice(SemanticSlice slice) =>
        (slice.Reactions.IsDefault ? [] : slice.Reactions).Where(reaction => reaction.RunsAs is not null)
            .Select(reaction => new SemanticVersionFeature(ReactionIdentity, StageExecutionCapability.Authorization, reaction.Id, "reaction", $"Reaction '{reaction.Name}' declares an ESM v10 execution identity."));

    /// <summary>
    /// Finds Automation slices the specification executor still refuses.
    /// </summary>
    /// <param name="slice">The slice.</param>
    /// <returns>The execution-only slice refusal.</returns>
    public static IEnumerable<SemanticVersionFeature> AutomationSlices(SemanticSlice slice)
    {
        if (slice.Kind == SemanticSliceKind.Automation)
        {
            yield return new(Automation, StageExecutionCapability.Reaction, slice.Id, "slice", $"Slice '{slice.Name}' is an ESM v6 Automation slice; targets must explicitly admit its reaction behavior.");
        }
    }

    /// <summary>
    /// Finds event reactions that targets must explicitly admit.
    /// </summary>
    /// <param name="slice">The slice.</param>
    /// <returns>The execution-only reaction refusals.</returns>
    public static IEnumerable<SemanticVersionFeature> EventReactionsInSlice(SemanticSlice slice) =>
        (slice.Reactions.IsDefault ? [] : slice.Reactions).Select(reaction => new SemanticVersionFeature(
            Automation, StageExecutionCapability.Reaction, reaction.Id, "reaction", $"Reaction '{reaction.Name}' is an ESM v6 automation construct; targets must explicitly admit its event-triggered behavior."));

    /// <summary>
    /// Finds public or foreign events declared elsewhere but referenced by a slice.
    /// </summary>
    /// <param name="slice">The referencing slice.</param>
    /// <param name="events">The application's indexed event contracts.</param>
    /// <returns>The refused event contracts.</returns>
    public static IEnumerable<SemanticVersionFeature> EventReferences(SemanticSlice slice, IReadOnlyDictionary<SemanticId, SemanticEventContract> events)
    {
        var references = slice.Projections.SelectMany(ProjectionReferencedEventNamesAreUnique.Contracts)
            .Concat(slice.Projections.Where(projection => projection.Target == SemanticProjectionTargetKind.Event).Select(projection => projection.ReadModel))
            .Concat(slice.Reducers.SelectMany(reducer => reducer.Transitions.Select(transition => transition.EventContract)))
            .Concat(slice.Reducers.Where(reducer => reducer.Target == SemanticProjectionTargetKind.Event).Select(reducer => reducer.ReadModel))
            .Concat(slice.Commands.SelectMany(command => command.Produces.Select(produced => produced.EventContract)))
            .Concat(ConstraintEvents(slice.Constraints))
            .Concat((slice.Reactions.IsDefault ? [] : slice.Reactions).SelectMany(reaction => reaction.Triggers)
                .SelectMany(trigger => trigger.Produces.Concat(trigger.Invokes.SelectMany(invocation => invocation.OnRefused.SelectMany(branch => branch.Produces))).Select(produced => produced.EventContract)
                    .Concat(trigger.Kind == SemanticReactionTriggerKind.Event ? [trigger.Source] : [])))
            .Concat((slice.Captures.IsDefault ? [] : slice.Captures).SelectMany(capture =>
                (capture.EventsSource?.Events ?? []).Concat(capture.Appends.Select(append => append.EventContract))
                    .Concat(capture.Children.SelectMany(children => children.Appends.Select(append => append.EventContract)))
                    .Concat(capture.Nested.SelectMany(nested => nested.Appends.Select(append => append.EventContract)))))
            .Concat(slice.Specifications.SelectMany(specification => specification.GivenEvents.Concat(specification.ThenEvents)
                .Select(occurrence => occurrence.EventContract)
                .Concat(specification.WhenAppended is { } appended ? [appended.EventContract] : [])))
            .Except(slice.Events.Select(@event => @event.Id));

        return EventReferences(references, events);
    }

    /// <summary>
    /// Finds public or foreign events named by the constraints a consumer selects.
    /// </summary>
    /// <param name="constraints">The selected constraints.</param>
    /// <param name="events">The application's indexed event contracts.</param>
    /// <returns>The refused event contracts.</returns>
    public static IEnumerable<SemanticVersionFeature> EventReferences(IEnumerable<SemanticConstraint> constraints, IReadOnlyDictionary<SemanticId, SemanticEventContract> events) =>
        EventReferences(ConstraintEvents(constraints), events);

    /// <summary>
    /// Finds the constructs one command uses: generated properties and a response.
    /// </summary>
    /// <param name="command">The command.</param>
    /// <returns>The refused constructs.</returns>
    public static IEnumerable<SemanticVersionFeature> InCommand(SemanticCommand command)
    {
        foreach (var feature in UnsupportedReadRequirements(command)) yield return feature;
        foreach (var feature in DescribedHandlersInCommand(command)) yield return feature;

        foreach (var route in RoutesInCommand(command))
        {
            yield return route;
        }

        foreach (var route in ProductionRoutesInCommand(command))
        {
            yield return route;
        }

        foreach (var feature in UnknownValidationRules(command.Validations, command.Id, "command", command.Name)) yield return feature;
        foreach (var feature in UnsupportedGenerated(command, null)) yield return feature;

        foreach (var reason in UnsupportedCommandResults(command))
        {
            yield return new(CommandResults, StageExecutionCapability.Command, command.Id, "command", $"Command '{command.Name}' {reason}; Stage cannot preserve that yet.");
        }
    }

    /// <summary>
    /// Finds the constructs one specification uses.
    /// </summary>
    /// <param name="specification">The specification.</param>
    /// <returns>The refused constructs.</returns>
    public static IEnumerable<SemanticVersionFeature> InSpecification(SemanticSpecification specification)
    {
        foreach (var route in RoutesInSpecification(specification))
        {
            yield return route;
        }

        if ((specification.GivenClock is not null && !SemanticSpecificationClock.TryGet(specification, out _)) || specification.WhenClock is not null || specification.WhenTrigger is not null ||
            specification.WhenCapture is not null || !specification.GivenCaptures.IsDefaultOrEmpty)
        {
            var capability = specification.GivenClock is not null || specification.WhenClock is not null ? StageExecutionCapability.Time : StageExecutionCapability.Reaction;
            yield return new(Automation, capability, specification.Id, "specification", $"Specification '{specification.Name}' uses an ESM v6 clock, trigger or capture, which Stage does not support yet.");
        }

        foreach (var feature in RedeliveryInSpecification(specification)) yield return feature;

        if (specification.ThenAbsentReadModels.Any(expected => expected.Key is not (SemanticTextValue or SemanticNumberValue or SemanticBooleanValue)))
        {
            yield return new(Absence, StageExecutionCapability.Projection, specification.Id, "specification", $"Specification '{specification.Name}' asserts read-model absence with a non-scalar key; the generated ReadModelScenario indexes instances by scalar event-source identities.");
        }
    }

    /// <summary>
    /// Retains the recovery feature query; shape-specific redelivery admission belongs to the scenario harness.
    /// </summary>
    /// <param name="specification">The specification.</param>
    /// <returns>No blanket redelivery refusal; the released scenario preserves recorded contexts.</returns>
    public static IEnumerable<SemanticVersionFeature> RedeliveryInSpecification(SemanticSpecification specification) => [];

    /// <summary>
    /// Finds a command route the renderer cannot render yet.
    /// </summary>
    /// <param name="command">The command.</param>
    /// <returns>The routed command, if any.</returns>
    public static IEnumerable<SemanticVersionFeature> RoutesInCommand(SemanticCommand command)
    {
        if (command.Route is not null)
        {
            yield return new(Routes, StageExecutionCapability.IdentityAllocation, command.Id, "command", $"Command '{command.Name}' has an event-source route, which the Cratis ESM planner cannot render yet.");
        }
    }

    /// <summary>
    /// Finds per-production route overrides whose rendered routing Stage has not proven equivalent to the reference.
    /// </summary>
    /// <param name="command">The command.</param>
    /// <returns>The refused command, if any.</returns>
    /// <remarks>
    /// Arc 22.112.1 (CommandTransactionAppender.ResolveRouting) takes a wrapper's EventStreamId when it differs from the
    /// EventStreamId.Default sentinel, and otherwise falls back to commandContext.GetEventStreamId(). Stage puts the command
    /// route on EventSourceAttribute and emits neither [EventStreamId] nor ICanProvideEventStreamId, so that fallback is
    /// null and the append uses EventStreamId.Default. Two shapes are refused because Stage has no generated-code
    /// conformance proof for them: an override on an unkeyed stream under a keyed command route, and a literal override
    /// stream id of "Default", which Arc cannot tell from an unset wrapper stream id. Both fail closed until proven.
    /// </remarks>
    public static IEnumerable<SemanticVersionFeature> ProductionRoutesInCommand(SemanticCommand command)
    {
        if (command.Route is { } commandRoute && IsKeyed(commandRoute) &&
            command.Produces.Any(produced => produced.Route is { } route && !IsKeyed(route)))
        {
            yield return new(ProductionRoutes, StageExecutionCapability.IdentityAllocation, command.Id, "command", $"Command '{command.Name}' overrides the route of a production with an unkeyed stream under a keyed command route, which Stage does not admit yet because its rendered routing is not proven equivalent to the reference execution.");
        }
        else if (command.Produces.Any(produced => produced.Route is { StreamId: SemanticValueExpression { Value: SemanticTextValue streamId } } && string.Equals(streamId.Value, ArcDefaultStreamId, StringComparison.Ordinal)))
        {
            yield return new(ProductionRoutes, StageExecutionCapability.IdentityAllocation, command.Id, "command", $"Command '{command.Name}' overrides the route of a production with the literal stream id '{ArcDefaultStreamId}', which is Arc's EventStreamId.Default sentinel, so Arc cannot tell it from an unset stream id and Stage does not admit it yet.");
        }
    }

    /// <summary>
    /// Finds routing fixtures and assertions the specification executor cannot execute yet.
    /// </summary>
    /// <param name="specification">The specification.</param>
    /// <returns>The routed specification, if any.</returns>
    public static IEnumerable<SemanticVersionFeature> RoutesInSpecification(SemanticSpecification specification)
    {
        if (specification.WhenAppended?.Route is not null || specification.GivenEvents.Concat(specification.ThenEvents).Any(occurrence => occurrence.Route is not null || occurrence.Unrouted))
        {
            yield return new(Routes, StageExecutionCapability.IdentityAllocation, specification.Id, "specification", $"Specification '{specification.Name}' uses event-source routing assertions, which the Cratis ESM planner cannot render yet.");
        }
    }

    /// <summary>
    /// Finds reducer filters that rendering admits but the Host and specification executor still refuse.
    /// </summary>
    /// <param name="slice">The slice.</param>
    /// <returns>The filtered reducers.</returns>
    public static IEnumerable<SemanticVersionFeature> ReducerFiltersInSlice(SemanticSlice slice) =>
        slice.Reducers.Where(reducer => reducer.From is not null).Select(reducer => new SemanticVersionFeature(
            ObserverFilters, StageExecutionCapability.Projection, reducer.ReadModel, "reducer", $"Reducer '{reducer.Name}' declares an ESM v10 observer filter, which Stage does not support yet."));

    internal static IEnumerable<SemanticVersionFeature> ImplementationGenerationInCommand(SemanticCommand command, IEnumerable<SemanticConcept> concepts, Func<string, string?>? ruleName = null)
    {
        foreach (var property in command.Properties.Where(property => property.IsGenerated))
        {
            var concept = concepts.FirstOrDefault(concept => concept.Id == property.Type.Target && concept.Generation?.Kind == "implementation");
            if (concept is not null)
            {
                var requirementId = concept.Generation!.RequirementId!;
                var rule = ruleName?.Invoke(requirementId) is { Length: > 0 } name ? $"'generate rule {name}'" : $"'generate rule' (requirement '{requirementId}')";
                yield return new(Generated, StageExecutionCapability.IdentityAllocation, command.Id, "command", $"Command '{command.Name}' generates '{property.Name}' with {rule} on concept '{concept.Name}'; the rule has an implementation body, which Stage's interpreter never runs or replaces with a generated value — render the application.");
            }
        }
    }

    internal static IEnumerable<SemanticVersionFeature> UnsupportedGenerated(SemanticCommand command, IEnumerable<SemanticConcept>? concepts)
    {
        foreach (var property in command.Properties.Where(property => property.IsGenerated))
        {
            if (UnsupportedGeneratedShape(property.Type) is { } shape)
            {
                yield return new(Generated, StageExecutionCapability.IdentityAllocation, command.Id, "command", $"Command '{command.Name}' generates '{property.Name}' as {shape}; Screenplay requires a generated property to be a required, non-collection generatable concept (PLAY0483).");
            }
            else if (concepts?.Any(concept => concept.Id == property.Type.Target && SemanticGenerationShapes.Supported(concept)) == false)
            {
                yield return new(Generated, StageExecutionCapability.IdentityAllocation, command.Id, "command", $"Command '{command.Name}' generates '{property.Name}'; Stage does not support the generation strategy or primitive of its concept.");
            }
        }
    }

    internal static IEnumerable<SemanticVersionFeature> UnsupportedReadRequirements(SemanticCommand command)
    {
        if (!command.Reads.IsDefaultOrEmpty && command.Requirements.Any(requirement => requirement.Severity != SemanticValidationSeverity.Error))
        {
            yield return new(ProtectedReadRequirementSeverity, StageExecutionCapability.Command, command.Id, "command", $"Command '{command.Name}' has a non-Error requirement on protected reads; Stage refuses this severity until Screenplay defines its semantics.");
        }
    }

    static IEnumerable<SemanticVersionFeature> UnknownValidationRules(
        IEnumerable<SemanticValidationRule> rules, SemanticId owner, string kind, string name) =>
        rules.Where(rule => !KnownValidationRule(rule.Kind)).Select(rule => new SemanticVersionFeature(
            "STAGE-ESM-005", StageExecutionCapability.Command, owner, kind, $"{kind} '{name}' declares unsupported validation rule kind '{rule.Kind}' on property '{rule.Property}'."));

    static bool KnownValidationRule(SemanticValidationRuleKind kind) => kind is
        SemanticValidationRuleKind.NotEmpty or SemanticValidationRuleKind.Maximum or SemanticValidationRuleKind.Minimum or
        SemanticValidationRuleKind.Equal or SemanticValidationRuleKind.NotEqual or SemanticValidationRuleKind.GreaterThan or
        SemanticValidationRuleKind.GreaterThanOrEqual or SemanticValidationRuleKind.LessThan or SemanticValidationRuleKind.LessThanOrEqual or
        SemanticValidationRuleKind.Length or SemanticValidationRuleKind.Matches or SemanticValidationRuleKind.Checksum or
        SemanticValidationRuleKind.AllGreaterThan or SemanticValidationRuleKind.AllGreaterThanOrEqual or
        SemanticValidationRuleKind.RulePredicate or SemanticValidationRuleKind.CodeValidation or
        SemanticValidationRuleKind.Contains or SemanticValidationRuleKind.NotContains or
        SemanticValidationRuleKind.StartsWith or SemanticValidationRuleKind.EndsWith or SemanticValidationRuleKind.DecimalPlaces;

    static IEnumerable<SemanticVersionFeature> NonEventTrigger(SemanticReaction reaction, SemanticReactionTrigger trigger)
    {
        if (trigger.Kind != SemanticReactionTriggerKind.Event)
        {
            yield return new(Automation, StageExecutionCapability.Reaction, reaction.Id, "reaction", $"Reaction '{reaction.Name}' uses trigger kind '{trigger.Kind}', which Stage does not support yet.");
        }
    }

    static IEnumerable<SemanticVersionFeature> MixedEffectsInReaction(SemanticReaction reaction)
    {
        foreach (var trigger in reaction.Triggers.Where(trigger => !trigger.Produces.IsDefaultOrEmpty && !trigger.Invokes.IsDefaultOrEmpty))
        {
            yield return new(MixedReactionEffects, StageExecutionCapability.Reaction, reaction.Id, "reaction", $"Reaction '{reaction.Name}' both produces events and invokes commands in one '{trigger.Kind}' trigger; a failed command would make Chronicle redeliver the trigger and append the reaction's events again until reactor appends are idempotent (Cratis/Stage#342).");
        }
        foreach (var group in reaction.Triggers.Where(trigger => trigger.Kind == SemanticReactionTriggerKind.Event).GroupBy(trigger => trigger.Source).Where(HasMixedTriggerGroup))
        {
            yield return new(MixedReactionEffects, StageExecutionCapability.Reaction, reaction.Id, "reaction", $"Reaction '{reaction.Name}' produces events and invokes commands across triggers on event '{group.Key}'; a failed command would make Chronicle redeliver the trigger and append the reaction's events again until reactor appends are idempotent (Cratis/Stage#342).");
        }
    }

    static bool HasMixedTriggerGroup(IGrouping<SemanticId, SemanticReactionTrigger> group) =>
        group.Count() > 1 && group.Any(trigger => !trigger.Produces.IsEmpty) && group.Any(trigger => !trigger.Invokes.IsEmpty);

    static IEnumerable<SemanticVersionFeature> UnsupportedReactionContexts(SemanticReaction reaction)
    {
        var contexts = ReactionContextExpressions(reaction).ToArray();
        foreach (var context in contexts.Where(context => IsCausedBy(context.Value)).Select(context => context.Value).Distinct())
        {
            var member = CausedByMember(context);
            yield return new("STAGE-ESM-013", StageExecutionCapability.Occurrence, reaction.Id, "reaction", $"Reaction '{reaction.Name}' maps $context.causedBy.{member}, which Stage does not support yet.");
        }
    }

    static IEnumerable<SemanticEventContextExpression> ReactionContextExpressions(SemanticReaction reaction) =>
        reaction.Triggers.SelectMany(trigger => trigger.Produces.Concat(trigger.Invokes.SelectMany(invocation => invocation.OnRefused.SelectMany(branch => branch.Produces))).SelectMany(produced =>
                produced.Mappings.Select(mapping => mapping.Source).Concat(produced.Destination is { } destination ? [destination] : []))
            .Concat(trigger.Invokes.SelectMany(invocation => invocation.Mappings.Select(mapping => mapping.Source))))
            .OfType<SemanticEventContextExpression>();

    static bool IsCausedBy(SemanticEventContextValueKind value) => value is
        SemanticEventContextValueKind.CausedBySubject or SemanticEventContextValueKind.CausedByName or SemanticEventContextValueKind.CausedByUserName;

    static string CausedByMember(SemanticEventContextValueKind value) => value switch
    {
        SemanticEventContextValueKind.CausedBySubject => "subject",
        SemanticEventContextValueKind.CausedByName => "name",
        _ => "userName"
    };

    static string? UnsupportedGeneratedShape(SemanticTypeReference type)
    {
        var noun = type.Kind switch
        {
            SemanticTypeReferenceKind.Concept => "concept",
            SemanticTypeReferenceKind.Primitive => "primitive",
            SemanticTypeReferenceKind.CompositeType => "composite type",
            _ => "unknown type"
        };

        return (type.Kind, type.IsOptional, type.IsCollection) switch
        {
            (SemanticTypeReferenceKind.Concept, false, false) => null,
            (_, true, true) => $"an optional collection of {noun}s",
            (_, true, false) => $"an optional {noun}",
            (_, false, true) => $"a collection of {noun}s",
            _ => noun == "unknown type" ? "an unknown type" : $"a {noun}"
        };
    }

    static IEnumerable<string> UnsupportedCommandResults(SemanticCommand command) =>
        UnsupportedGeneratedCombination(command).Concat(UnsupportedResponse(command));

    static IEnumerable<string> UnsupportedGeneratedCombination(SemanticCommand command)
    {
        var generated = command.Properties.Where(property => property.IsGenerated).ToArray();
        if (generated.Length == 0) yield break;
        if (generated.Any(property => property.IsIdentifier) && command.Produces.Any(produced => produced.Destination is null))
        {
            yield return "combines a generated identifier with a destination-less production, which uses legacy destination allocation";
        }
        foreach (var property in generated.Where(property => command.Requirements.Any(requirement => ReadsGenerated(requirement.Condition, property))))
        {
            yield return $"has a requirement reading generated property '{property.Name}', which does not exist during request validation";
        }
        foreach (var property in generated.Where(property => command.Validations.Any(rule => rule.Property == property.Id)))
        {
            yield return $"has a rule on generated property '{property.Name}', which does not exist during request validation";
        }
    }

    static bool ReadsGenerated(SemanticCondition condition, SemanticProperty generated) => condition switch
    {
        SemanticLogicalCondition logical => ReadsGenerated(logical.Left, generated) || ReadsGenerated(logical.Right, generated),
        SemanticComparison comparison => generated.Id == comparison.Left.Property || generated.Id == comparison.Right.Property,
        _ => false
    };

    // Response member spelling is renderer-specific, but the Host and executor share this admission, so only
    // collisions that no type-name escape can resolve are refused here: duplicate or record-reserved field names.
    static IEnumerable<string> UnsupportedResponse(SemanticCommand command) => command.Response switch
    {
        SemanticScalarCommandResponse scalar => UnsupportedResponseSource(command, "response", scalar.Source, scalar.Type),
        SemanticRecordCommandResponse record => record.Fields.SelectMany(field => UnsupportedResponseSource(command, $"response field '{field.Name}'", field.Source, field.Type))
            .Concat(GeneratedPascalCase.ResponseMembersAreUnique(record.Fields.Select(field => field.Name))
                ? []
                : ["declares response fields whose generated C# members collide with each other or with a record member"]),
        _ => []
    };

    static IEnumerable<string> UnsupportedResponseSource(SemanticCommand command, string response, SemanticId source, SemanticTypeReference type)
    {
        if (type.IsCollection)
        {
            yield return $"returns a collection as {response}";
        }
        else if (!command.Properties.Any(property => property.Id == source && property.Type == type))
        {
            yield return $"returns {response} from a source that is not a command property of the same type";
        }
    }

    static IEnumerable<SemanticId> ConstraintEvents(IEnumerable<SemanticConstraint> constraints) =>
        constraints.SelectMany(constraint => constraint.Targets.Select(target => target.EventContract).Concat(constraint.ReleasedBy));

    static IEnumerable<SemanticVersionFeature> EventReferences(IEnumerable<SemanticId> references, IReadOnlyDictionary<SemanticId, SemanticEventContract> events)
    {
        foreach (var id in references.Distinct())
        {
            if (events.TryGetValue(id, out var @event) && InEvent(@event) is { } feature) yield return feature;
        }
    }

    static SemanticVersionFeature? InEvent(SemanticEventContract @event)
    {
        if (@event.Origin is not null)
        {
            return new(ForeignEvents, StageExecutionCapability.ExternalEffect, @event.Id, "event", $"Event '{@event.Name}' originates from '{@event.Origin}' (ESM v9), which Stage does not support yet.");
        }

        return @event.Visibility == SemanticEventVisibility.Public
            ? new(PublicEvents, StageExecutionCapability.ExternalEffect, @event.Id, "event", $"Event '{@event.Name}' is an ESM v9 public contract, which Stage cannot publish yet.")
            : null;
    }

    static bool IsKeyed(SemanticCommandRoute route) => route.StreamId is not null || !route.StreamIdParts.IsDefaultOrEmpty;

    static IEnumerable<SemanticSlice> Slices(SemanticFeature feature) => feature.Slices.Concat(feature.Features.SelectMany(Slices));
}
