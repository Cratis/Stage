// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Stage.Contracts.Rendering;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

internal static partial class SemanticSpecificationAdmission
{
    static IEnumerable<SemanticSpecification> WithRenderableGeneratedValues(
        SemanticApplicationContext context,
        IEnumerable<SemanticSpecification> specifications,
        ICollection<ArtifactRenderDiagnostic> diagnostics)
    {
        foreach (var specification in specifications)
        {
            if (InvalidStrategyFixture(context, specification) is { } message)
            {
                diagnostics.Add(new("STAGE-ESM-011", ArtifactRenderDiagnosticSeverity.Error, message, specification.Id));
            }
            else if (MissingConstrainedGeneratedFixture(context, specification) is { } constraintMessage)
            {
                diagnostics.Add(new(SemanticVersionFeatures.CommandResults, ArtifactRenderDiagnosticSeverity.Error, constraintMessage, specification.Id));
            }
            else if (NeedsPinnedGeneratedValues(context, specification))
            {
                diagnostics.Add(new(
                    SemanticVersionFeatures.CommandResults,
                    ArtifactRenderDiagnosticSeverity.Error,
                    $"Specification '{specification.Name}' needs pinned generated values for {PinReason(context.Commands[specification.When!.Command])}.",
                    specification.Id));
            }
            else
            {
                yield return specification;
            }
        }
    }

    static string PinReason(SemanticCommand command) => command.Produces.Any(produced => produced.When is not null)
        ? "production conditions, unordered or projected expectations"
        : "unordered or projected expectations";

    static string? InvalidStrategyFixture(SemanticApplicationContext context, SemanticSpecification specification)
    {
        if (!context.Request.Model.SemanticVersion.IsAtLeast(SemanticVersion.V15) || specification.When is not { } action ||
            !context.Commands.TryGetValue(action.Command, out var command))
        {
            return null;
        }
        foreach (var fixture in action.GeneratedValues)
        {
            var property = command.Properties.SingleOrDefault(property => property.Id == fixture.TargetProperty && property.IsGenerated);
            if (property is not null && !SemanticGenerationShapes.Matches(context.Concepts[property.Type.Target], fixture.Value))
            {
                return $"Specification '{specification.Name}' cannot render: {SemanticGenerationShapes.FixtureMessage(property.Name)}";
            }
        }

        return null;
    }

    static bool NeedsPinnedGeneratedValues(SemanticApplicationContext context, SemanticSpecification specification)
    {
        if (specification.When is not { } action) return false;
        if (!context.Commands.TryGetValue(action.Command, out var command)) return false;
        var conditionProperties = command.Produces.Where(produced => produced.When is not null)
            .SelectMany(produced => SemanticProductionSelection.Properties(produced.When!)).ToHashSet();
        var unpinned = UnpinnedGeneratedProperties(command, action).ToArray();
        if (unpinned.Any(property => conditionProperties.Contains(property.Id))) return true;

        return HasProjectedOrUnorderedExpectations(specification) && unpinned.Length > 0;
    }

    static IEnumerable<SemanticProperty> UnpinnedGeneratedProperties(SemanticCommand command, SemanticSpecificationCommand action) =>
        command.Properties.Where(property => property.IsGenerated)
            .Where(property => !action.GeneratedValues.Any(value => value.TargetProperty == property.Id));

    static bool HasProjectedOrUnorderedExpectations(SemanticSpecification specification) =>
        specification.ThenEventsInAnyOrder || !specification.ThenReadModels.IsEmpty || !specification.ThenQueries.IsEmpty;

    static bool HasDestinationValue(SemanticProperty property, SemanticValue? value, SemanticEventSourceIdentity? source, IEnumerable<SemanticSpecificationEvent> expected)
    {
        if (value is not null) return true;
        if (!property.IsGenerated) return false;

        return source is null && expected.All(occurrence => occurrence.EventSource is null);
    }
}
