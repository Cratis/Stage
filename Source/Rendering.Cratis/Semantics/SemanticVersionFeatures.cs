// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Stage.Contracts.Specifications.Semantic;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

/// <summary>
/// One ESM v5–v8 construct Stage can neither render nor execute yet.
/// </summary>
/// <param name="Code">The typed STAGE-ESM diagnostic code.</param>
/// <param name="Capability">The execution capability the construct needs.</param>
/// <param name="Artifact">The declaration that uses the construct.</param>
/// <param name="Kind">The kind of declaration: trigger, slice, reaction, capture, command or specification.</param>
/// <param name="Message">What the construct is and why it is refused.</param>
internal sealed record SemanticVersionFeature(string Code, StageExecutionCapability Capability, SemanticId Artifact, string Kind, string Message);

/// <summary>
/// Finds the ESM v5–v8 constructs that renderer admission, the specification executor and the Host runtime refuse.
/// </summary>
/// <remarks>
/// One detection serves all three (the Host and the specification executor compile this file), so a construct cannot
/// be refused by one and silently skipped by another. Admitting a version never admits these constructs.
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
    /// The code for unaudited ESM v8 event sources, streams and routes.
    /// </summary>
    internal const string Routes = "STAGE-ESM-016";

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
        .Concat((application.EventSources.IsDefault ? [] : application.EventSources).Select(source => new SemanticVersionFeature(
            Routes, StageExecutionCapability.IdentityAllocation, source.Id, "eventsource", $"Event source '{source.Name}' declares ESM v8 routing, which Stage does not support yet.")));

    /// <summary>
    /// Finds the structural automation constructs of one slice: its kind, reactions and captures.
    /// </summary>
    /// <param name="slice">The slice.</param>
    /// <returns>The refused constructs.</returns>
    public static IEnumerable<SemanticVersionFeature> InSlice(SemanticSlice slice)
    {
        if (slice.Kind is SemanticSliceKind.Automation or SemanticSliceKind.Translate)
        {
            yield return new(Automation, StageExecutionCapability.Reaction, slice.Id, "slice", $"Slice '{slice.Name}' is an ESM v6 {slice.Kind} slice; Stage renders only StateChange and StateView slices.");
        }

        foreach (var reaction in slice.Reactions.IsDefault ? [] : slice.Reactions)
        {
            yield return new(Automation, StageExecutionCapability.Reaction, reaction.Id, "reaction", $"Reaction '{reaction.Name}' is an ESM v6 automation construct, which Stage does not support yet.");
        }

        foreach (var capture in slice.Captures.IsDefault ? [] : slice.Captures)
        {
            yield return new(Automation, StageExecutionCapability.ExternalEffect, capture.Id, "capture", $"Capture '{capture.Name}' is an ESM v6 translation construct, which Stage does not support yet.");
        }
    }

    /// <summary>
    /// Finds the constructs one command uses: generated properties and a response.
    /// </summary>
    /// <param name="command">The command.</param>
    /// <returns>The refused constructs.</returns>
    public static IEnumerable<SemanticVersionFeature> InCommand(SemanticCommand command)
    {
        if (command.Route is not null)
        {
            yield return new(Routes, StageExecutionCapability.IdentityAllocation, command.Id, "command", $"Command '{command.Name}' declares an ESM v8 route, which Stage does not support yet.");
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
        if (specification.WhenAppended?.Route is not null || specification.GivenEvents.Concat(specification.ThenEvents).Any(occurrence => occurrence.Route is not null || occurrence.Unrouted))
        {
            yield return new(Routes, StageExecutionCapability.IdentityAllocation, specification.Id, "specification", $"Specification '{specification.Name}' asserts ESM v8 routing, which Stage does not support yet.");
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

    static IEnumerable<SemanticSlice> Slices(SemanticFeature feature) => feature.Slices.Concat(feature.Features.SelectMany(Slices));
}
