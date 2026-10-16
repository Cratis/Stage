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
    /// The code for ESM v10 per-production route overrides.
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
    /// The code for atomic reaction batches, whose production boundary no Stage engine preserves yet.
    /// </summary>
    internal const string AtomicReactionBatches = "STAGE-ESM-043";

    /// <summary>
    /// The code for protected command reads, which the renderer does not preserve yet.
    /// </summary>
    internal const string ProtectedCommandReads = "STAGE-ESM-044";

    /// <summary>
    /// The code for pending command handlers, which no Stage engine implements yet.
    /// </summary>
    internal const string PendingCommandHandler = "STAGE-IMPL-002";

    /// <summary>
    /// Checks the language and semantic version pair Stage has audited.
    /// </summary>
    /// <param name="model">The executable model.</param>
    /// <returns>Whether Stage supports the version pair.</returns>
    public static bool Supports(ExecutableSemanticModel model) =>
        EsmSchemaV21Support.Supports(model.LanguageVersion, model.SemanticVersion) ||
        (model.LanguageVersion == LanguageVersion.V23 && model.SemanticVersion == SemanticVersion.V23) ||
        (model.LanguageVersion == LanguageVersion.V24 && model.SemanticVersion == SemanticVersion.V24) ||
        (model.LanguageVersion == LanguageVersion.V25 && model.SemanticVersion == SemanticVersion.V25);

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
        (application.Triggers.IsDefault ? [] : application.Triggers).Select(trigger => new SemanticVersionFeature(
            Automation, StageExecutionCapability.Reaction, trigger.Id, "trigger", $"Application trigger '{trigger.Name}' is an ESM v6 automation construct, which Stage does not support yet."))
        .Concat(ReactionBatchesInApplication(application))
        .Concat(ProtectedReadsInApplication(application))
        .Concat(Slices(application).SelectMany(slice => slice.Commands).SelectMany(DescribedHandlersInCommand))
        .Concat(RoutesInApplication(application))
        .Concat(UnsupportedConcepts(application.Concepts))
        .Concat(application.Concepts.SelectMany(concept => UnknownValidationRules(concept.Validations, concept.Id, "concept", concept.Name)))
        .Concat(Slices(application).SelectMany(slice => slice.Commands).SelectMany(command => UnknownValidationRules(command.Validations, command.Id, "command", command.Name)))
        .Concat(Slices(application).SelectMany(slice => slice.Commands).SelectMany(command => UnsupportedGenerated(command, application.Concepts)));

    /// <summary>
    /// Finds an application-wide reaction production boundary no Stage engine preserves yet.
    /// </summary>
    /// <param name="application">The semantic application.</param>
    /// <returns>The refused reaction production mode.</returns>
    public static IEnumerable<SemanticVersionFeature> ReactionBatchesInApplication(SemanticApplication application)
    {
        if (application.ReactionProductionMode is { } mode)
        {
            yield return new(AtomicReactionBatches, StageExecutionCapability.Reaction, application.Id, "application", $"Application '{application.Name}' declares reaction batches {mode.ToString().ToLowerInvariant()} (ESM v22); Stage does not preserve that production boundary yet (Cratis/Stage#422).");
        }
    }

    /// <summary>
    /// Finds an application-wide protected command-read boundary the renderer must refuse.
    /// </summary>
    /// <param name="application">The semantic application.</param>
    /// <returns>The refused command-read mode.</returns>
    public static IEnumerable<SemanticVersionFeature> ProtectedReadsInApplication(SemanticApplication application)
    {
        if (application.CommandReadMode is not null)
        {
            yield return new(ProtectedCommandReads, StageExecutionCapability.Command, application.Id, "application", $"Application '{application.Name}' declares command reads protected (ESM v25); The Cratis renderer does not preserve protected decision reads yet.");
        }
    }

    /// <summary>
    /// Finds command reads and read-backed conditions or mappings the renderer must refuse.
    /// </summary>
    /// <param name="command">The command.</param>
    /// <returns>The refused command, if any.</returns>
    public static IEnumerable<SemanticVersionFeature> ProtectedReadsInCommand(SemanticCommand command)
    {
        if (!command.Reads.IsDefaultOrEmpty || command.Requirements.Any(requirement => ReadsView(requirement.Condition)) ||
            command.Produces.Any(produced => (produced.When is { } condition && ReadsView(condition)) ||
                produced.Mappings.Any(mapping => mapping.Source is SemanticReadPathExpression) || produced.Destination is SemanticReadPathExpression) ||
            command.Destination?.Value is SemanticReadPathExpression)
        {
            yield return new(ProtectedCommandReads, StageExecutionCapability.Command, command.Id, "command", $"Command '{command.Name}' uses protected decision reads or read paths (ESM v25); The Cratis renderer does not preserve that decision boundary yet.");
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

                if (trigger.Kind != SemanticReactionTriggerKind.Event)
                {
                    yield return new(Automation, StageExecutionCapability.Reaction, reaction.Id, "reaction", $"Reaction '{reaction.Name}' uses trigger kind '{trigger.Kind}', which Stage does not support yet.");
                }
            }
        }

        foreach (var feature in EventReactionsInSlice(slice)) yield return feature;
    }

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
        foreach (var feature in ProtectedReadsInCommand(command)) yield return feature;
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

        if (UnsupportedCommandResults(command))
        {
            yield return new(CommandResults, StageExecutionCapability.Command, command.Id, "command", $"Command '{command.Name}' combines a response with routing, generated identifiers with legacy destination allocation, rules or requirements reading generated properties, or an unsupported response source or shape; Stage cannot preserve that combination yet.");
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

        if (!specification.ThenAbsentReadModels.IsDefaultOrEmpty)
        {
            yield return new(Absence, StageExecutionCapability.Projection, specification.Id, "specification", $"Specification '{specification.Name}' asserts a read model is absent (ESM v5); the generated ReadModelScenario exposes only a materialized record, so Stage does not support absence yet.");
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
    /// Finds per-production route overrides that no Stage execution path supports yet.
    /// </summary>
    /// <param name="command">The command.</param>
    /// <returns>The refused command, if any.</returns>
    public static IEnumerable<SemanticVersionFeature> ProductionRoutesInCommand(SemanticCommand command)
    {
        if (command.Produces.Any(produced => produced.Route is not null))
        {
            yield return new(ProductionRoutes, StageExecutionCapability.IdentityAllocation, command.Id, "command", $"Command '{command.Name}' declares an ESM v10 production route override, which Stage does not support yet.");
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

    internal static IEnumerable<SemanticVersionFeature> UnsupportedConcepts(IEnumerable<SemanticConcept> concepts)
    {
        foreach (var concept in concepts)
        {
            var strategy = concept.Generation?.Kind switch
            {
                "implementation" => "rule",
                _ => null
            };
            if (strategy is not null)
            {
                yield return new(Generated, StageExecutionCapability.IdentityAllocation, concept.Id, "concept", $"Concept '{concept.Name}' uses 'generate {strategy}', which Stage does not support yet.");
            }
        }
    }

    internal static IEnumerable<SemanticVersionFeature> UnsupportedGenerated(SemanticCommand command, IEnumerable<SemanticConcept>? concepts)
    {
        foreach (var property in command.Properties.Where(property => property.IsGenerated).Where(property => UnsupportedGeneratedType(property.Type, concepts)))
        {
            yield return new(Generated, StageExecutionCapability.IdentityAllocation, command.Id, "command", $"Command '{command.Name}' generates '{property.Name}'; Stage renders only non-optional, non-collection concepts with admitted generation strategies.");
        }
        if (concepts is not null)
        {
            foreach (var concept in concepts.Where(concept => command.Properties.Any(property => property.IsGenerated && property.Type.Target == concept.Id)))
            {
                if (GeneratedPascalCase.From(concept.Name) == "New")
                {
                    yield return new(CommandResults, StageExecutionCapability.Command, command.Id, "command", $"Generated concept '{concept.Name}' collides with its New factory.");
                }
            }
        }
    }

    internal static IEnumerable<SemanticVersionFeature> UnsupportedReadRequirements(SemanticCommand command)
    {
        if (!command.Reads.IsDefaultOrEmpty && command.Requirements.Any(requirement => requirement.Severity != SemanticValidationSeverity.Error))
        {
            yield return new(ProtectedCommandReads, StageExecutionCapability.Command, command.Id, "command", $"Command '{command.Name}' has a non-Error requirement on protected reads; Stage refuses this severity until Screenplay defines its semantics.");
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
        SemanticValidationRuleKind.StartsWith or SemanticValidationRuleKind.EndsWith;

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
        if (contexts.Any(context => context.Value == SemanticEventContextValueKind.EventSourceIdentity))
        {
            yield return new("STAGE-ESM-013", StageExecutionCapability.IdentityAllocation, reaction.Id, "reaction", $"Reaction '{reaction.Name}' maps $context.eventSourceId, which Stage does not support yet.");
        }
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

    static bool UnsupportedGeneratedType(SemanticTypeReference type, IEnumerable<SemanticConcept>? concepts)
    {
        if (type is not { Kind: SemanticTypeReferenceKind.Concept, IsOptional: false, IsCollection: false }) return true;

        return concepts?.Any(concept => concept.Id == type.Target && SemanticGenerationShapes.Supported(concept)) == false;
    }

    static bool UnsupportedCommandResults(SemanticCommand command) =>
        UnsupportedGeneratedCombination(command) || UnsupportedResponse(command);

    static bool UnsupportedGeneratedCombination(SemanticCommand command)
    {
        var generated = command.Properties.Where(property => property.IsGenerated).ToArray();
        if (HasRoutedResults(command)) return true;
        if (generated.Length == 0) return false;
        if (generated.Any(property => property.IsIdentifier) && command.Produces.Any(produced => produced.Destination is null)) return true;

        return command.Requirements.Any(requirement => ReadsGenerated(requirement.Condition, generated)) ||
            command.Validations.Any(rule => generated.Any(property => property.Id == rule.Property));
    }

    static bool ReadsView(SemanticCondition condition) => condition switch
    {
        SemanticLogicalCondition logical => ReadsView(logical.Left) || ReadsView(logical.Right),
        SemanticComparison comparison => comparison.Left.Read is not null || comparison.Right.Read is not null,
        _ => false
    };

    static bool ReadsGenerated(SemanticCondition condition, IReadOnlyList<SemanticProperty> generated) => condition switch
    {
        SemanticLogicalCondition logical => ReadsGenerated(logical.Left, generated) || ReadsGenerated(logical.Right, generated),
        SemanticComparison comparison => generated.Any(property => property.Id == comparison.Left.Property || property.Id == comparison.Right.Property),
        _ => false
    };

    static bool HasRoutedResults(SemanticCommand command) =>
        command.Route is not null && command.Response is not null;

    static bool UnsupportedResponse(SemanticCommand command) => command.Response switch
    {
        SemanticScalarCommandResponse scalar => UnsupportedResponseSource(command, scalar.Source, scalar.Type),
        SemanticRecordCommandResponse record =>
            record.Fields.Any(field => UnsupportedResponseSource(command, field.Source, field.Type)) ||
            !GeneratedPascalCase.RecordMembersAreUnique(command.Name + "Response", record.Fields.Select(field => field.Name)),
        _ => false
    };

    static bool UnsupportedResponseSource(SemanticCommand command, SemanticId source, SemanticTypeReference type) =>
        type.IsCollection || type.Kind == SemanticTypeReferenceKind.CompositeType ||
        !command.Properties.Any(property => property.Id == source && property.Type == type);

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

    static IEnumerable<SemanticSlice> Slices(SemanticFeature feature) => feature.Slices.Concat(feature.Features.SelectMany(Slices));
}
