// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

/// <summary>
/// Scopes specification admission to the behavior the declaring slice owns.
/// </summary>
/// <remarks>
/// Screenplay decision 0115: a specification runs only the slice that declares it. Other slices' reactions, captures and
/// publications do not run, and a command another slice owns, invoked by one of the slice's own reactions, is decided
/// without its facts being kept. This mirrors <c language="csharp">SemanticExecutionPlan.IsolatedTo</c> and <c language="csharp">Runs</c> in Screenplay.
/// </remarks>
internal static partial class SemanticSpecificationAdmission
{
    /// <summary>
    /// Gets the reactions a specification runs: those of the slice that declares it.
    /// </summary>
    /// <param name="context">The indexed semantic application.</param>
    /// <param name="specification">The specification.</param>
    /// <returns>The declaring slice's reactions, or every slice's when no slice declares the specification.</returns>
    internal static IEnumerable<SemanticReaction> RunningReactions(SemanticApplicationContext context, SemanticSpecification specification) =>
        (Owner(context, specification) is { } owner ? [owner.Slice] : SemanticVersionFeatures.Slices(context.Application))
            .SelectMany(slice => slice.Reactions.IsDefault ? [] : slice.Reactions);

    /// <summary>
    /// Gets the event triggers of the reactions a specification runs.
    /// </summary>
    /// <param name="context">The indexed semantic application.</param>
    /// <param name="specification">The specification.</param>
    /// <returns>The running reactions' event triggers.</returns>
    internal static IEnumerable<SemanticReactionTrigger> EventReactionTriggers(SemanticApplicationContext context, SemanticSpecification specification) =>
        RunningReactions(context, specification).SelectMany(reaction => reaction.Triggers).Where(trigger => trigger.Kind == SemanticReactionTriggerKind.Event);

    /// <summary>
    /// Gets whether a command invoked by a reaction keeps its facts in the specification's run: only when the declaring slice owns it.
    /// </summary>
    /// <param name="context">The indexed semantic application.</param>
    /// <param name="specification">The specification.</param>
    /// <param name="command">The invoked command.</param>
    /// <returns>Whether the invoked command's events are appended in the run.</returns>
    internal static bool AdoptsInvocation(SemanticApplicationContext context, SemanticSpecification specification, SemanticId command) =>
        Owner(context, specification) is not { } owner || owner.Declares(command);

    static LocatedSemanticSlice? Owner(SemanticApplicationContext context, SemanticSpecification specification) =>
        context.AllSlices.SingleOrDefault(located => located.Slice.Specifications.Any(candidate => candidate.Id == specification.Id));
}
