// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Stage.Contracts.Rendering;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

internal static partial class SemanticSpecificationAdmission
{
    static IEnumerable<SemanticSpecification> WithRenderableGeneratedValues(
        SemanticApplicationContext context,
        SemanticSlice slice,
        ICollection<ArtifactRenderDiagnostic> diagnostics)
    {
        foreach (var specification in slice.Specifications)
        {
            if (NeedsPinnedGeneratedValues(context, specification))
            {
                diagnostics.Add(new(
                    SemanticVersionFeatures.CommandResults,
                    ArtifactRenderDiagnosticSeverity.Error,
                    $"Specification '{specification.Name}' needs pinned generated values for unordered or projected expectations.",
                    specification.Id));
            }
            else
            {
                yield return specification;
            }
        }
    }

    static bool NeedsPinnedGeneratedValues(SemanticApplicationContext context, SemanticSpecification specification)
    {
        if (specification.When is not { } action) return false;
        if (!context.Commands.TryGetValue(action.Command, out var command)) return false;
        if (!HasProjectedOrUnorderedExpectations(specification)) return false;

        return command.Properties.Where(property => property.IsGenerated)
            .Any(property => !action.GeneratedValues.Any(value => value.TargetProperty == property.Id));
    }

    static bool HasProjectedOrUnorderedExpectations(SemanticSpecification specification) =>
        specification.ThenEventsInAnyOrder || !specification.ThenReadModels.IsEmpty || !specification.ThenQueries.IsEmpty;

    static bool HasDestinationValue(SemanticProperty property, SemanticValue? value, SemanticEventSourceIdentity? source, IEnumerable<SemanticSpecificationEvent> expected)
    {
        if (value is not null) return true;
        if (!property.IsGenerated) return false;

        return source is null && expected.All(occurrence => occurrence.EventSource is null);
    }
}
