// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Stage.Contracts.Rendering;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

/// <summary>
/// Admits semantic specifications the generated Cratis scenario family can preserve.
/// </summary>
internal static partial class SemanticSpecificationAdmission
{
    /// <summary>
    /// Validates the specifications declared by one slice.
    /// </summary>
    /// <param name="context">The indexed semantic application.</param>
    /// <param name="slice">The declaring slice.</param>
    /// <param name="diagnostics">The diagnostics to append to.</param>
    public static void Validate(
        SemanticApplicationContext context,
        SemanticSlice slice,
        ICollection<ArtifactRenderDiagnostic> diagnostics)
    {
        foreach (var specification in slice.Specifications)
        {
            var valid = HasRenderableCallerAndCommand(context, specification, out var command) &&
                HasRenderableGivenEvents(context, specification) && specification.GivenReadModels.IsEmpty &&
                ValuesMatch(specification.When!.Values, command?.Properties ?? []) &&
                HasOneOutcome(specification) && HasSupportedCounts(specification) &&
                specification.ThenEvents.All(expected => EventMatches(context, expected) &&
                    command!.Produces.Any(produced => produced.EventContract == expected.EventContract)) &&
                specification.ThenReadModels.All(expected => ReadModelMatches(context, expected) &&
                    HasExpectedProjectionEvent(context, specification, expected)) &&
                specification.ThenQueries.All(expected => QueryMatches(context, expected)) &&
                specification.ThenErrors.All(_ => _.Code is null) &&
                HasRenderableEventSources(context, specification, command!);

            if (!valid)
            {
                diagnostics.Add(new(
                    "STAGE-ESM-011",
                    ArtifactRenderDiagnosticSeverity.Error,
                    $"Specification '{specification.Name}' exceeds the first generated Cratis specification capability.",
                    specification.Id));
            }
        }
    }
}
