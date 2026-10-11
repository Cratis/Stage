// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Stage.Contracts.Specifications.Semantic;
using Cratis.Stage.Rendering.Cratis.Semantics.Projections;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

/// <summary>
/// One ESM v5–v9 construct a Stage execution or rendering path refuses.
/// </summary>
/// <param name="Code">The typed STAGE-ESM diagnostic code.</param>
/// <param name="Capability">The execution capability the construct needs.</param>
/// <param name="Artifact">The declaration that uses the construct.</param>
/// <param name="Kind">The kind of declaration: trigger, slice, reaction, capture, command or specification.</param>
/// <param name="Message">What the construct is and why it is refused.</param>
internal sealed record SemanticVersionFeature(string Code, StageExecutionCapability Capability, SemanticId Artifact, string Kind, string Message);

/// <summary>
/// Finds the ESM v5–v9 constructs that renderer admission, the specification executor and the Host runtime refuse.
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
    /// The code for ESM v8 event sources, streams and routes, shared with renderer admission and the semantic surface ledger.
    /// </summary>
    internal const string Routes = "STAGE-ESM-030";

    /// <summary>
    /// The code for ESM v9 public-event publication.
    /// </summary>
    internal const string PublicEvents = "STAGE-ESM-031";

    /// <summary>
    /// The code for ESM v9 foreign events and events-source captures.
    /// </summary>
    internal const string ForeignEvents = "STAGE-ESM-032";

    /// <summary>
    /// Checks the language and semantic version pair Stage has audited.
    /// </summary>
    /// <param name="model">The executable model.</param>
    /// <returns>Whether Stage supports the version pair.</returns>
    public static bool Supports(ExecutableSemanticModel model) => EsmSchemaV9Support.Supports(model.LanguageVersion, model.SemanticVersion);

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
        .Concat(RoutesInApplication(application));

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
        if (slice.Kind is SemanticSliceKind.Automation or SemanticSliceKind.Translate)
        {
            var direction = slice.Kind == SemanticSliceKind.Translate ? $" ({slice.Direction?.ToString() ?? "Inbound"})" : string.Empty;
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

        foreach (var reaction in slice.Reactions.IsDefault ? [] : slice.Reactions)
        {
            yield return new(Automation, StageExecutionCapability.Reaction, reaction.Id, "reaction", $"Reaction '{reaction.Name}' is an ESM v6 automation construct, which Stage does not support yet.");
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
                .SelectMany(trigger => trigger.Produces.Select(produced => produced.EventContract)
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
        foreach (var route in RoutesInCommand(command))
        {
            yield return route;
        }

        foreach (var property in command.Properties.Where(property => property.IsGenerated))
        {
            yield return new(Generated, StageExecutionCapability.IdentityAllocation, command.Id, "command", $"Command '{command.Name}' generates '{property.Name}' (ESM v7), which Stage does not support yet.");
        }

        if (command.Response is not null)
        {
            yield return new(Responses, StageExecutionCapability.Command, command.Id, "command", $"Command '{command.Name}' declares a response (ESM v7), which Stage does not support yet.");
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

        if (specification.GivenClock is not null || specification.WhenClock is not null || specification.WhenTrigger is not null ||
            specification.WhenCapture is not null || !specification.GivenCaptures.IsDefaultOrEmpty)
        {
            var capability = specification.GivenClock is not null || specification.WhenClock is not null ? StageExecutionCapability.Time : StageExecutionCapability.Reaction;
            yield return new(Automation, capability, specification.Id, "specification", $"Specification '{specification.Name}' uses an ESM v6 clock, trigger or capture, which Stage does not support yet.");
        }

        if (!specification.ThenAbsentReadModels.IsDefaultOrEmpty)
        {
            yield return new(Absence, StageExecutionCapability.Projection, specification.Id, "specification", $"Specification '{specification.Name}' asserts a read model is absent (ESM v5); the generated ReadModelScenario exposes only a materialized record, so Stage does not support absence yet.");
        }

        if (specification.When is { GeneratedValues.IsDefaultOrEmpty: false })
        {
            yield return new(Generated, StageExecutionCapability.IdentityAllocation, specification.Id, "specification", $"Specification '{specification.Name}' supplies generated values (ESM v7), which Stage does not support yet.");
        }

        if (specification.ThenReturns is not null)
        {
            yield return new(Responses, StageExecutionCapability.Command, specification.Id, "specification", $"Specification '{specification.Name}' asserts a command response (ESM v7), which Stage does not support yet.");
        }
    }

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
