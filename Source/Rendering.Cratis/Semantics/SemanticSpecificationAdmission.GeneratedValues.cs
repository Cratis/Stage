// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Generation;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Specifications.Commands;

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
            if (UnsupportedGeneratedRule(context, specification) is { } refusal)
            {
                diagnostics.Add(refusal);
            }
            else if (InvalidStrategyFixture(context, specification) is { } message)
            {
                diagnostics.Add(new("STAGE-ESM-011", ArtifactRenderDiagnosticSeverity.Error, message, specification.Id));
            }
            else if (UnpinnedRuleBearingConcept(context, specification) is { } ruleMessage)
            {
                diagnostics.Add(new(SemanticVersionFeatures.CommandResults, ArtifactRenderDiagnosticSeverity.Error, ruleMessage, specification.Id));
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

    static ArtifactRenderDiagnostic? UnsupportedGeneratedRule(SemanticApplicationContext context, SemanticSpecification specification)
    {
        if (specification.When is not { } action || !context.Commands.TryGetValue(action.Command, out var command)) return null;
        foreach (var fixture in action.GeneratedValues)
        {
            var property = command.Properties.SingleOrDefault(property => property.Id == fixture.TargetProperty && property.IsGenerated);
            if (property is null) continue;
            var concept = context.Concepts[property.Type.Target];
            if (concept.Validations.FirstOrDefault(rule => !SemanticRuleValues.CanEvaluate(rule.Kind)) is { } rule)
            {
                return new(
                    "STAGE-ESM-005",
                    ArtifactRenderDiagnosticSeverity.Error,
                    $"Specification '{specification.Name}' cannot render: generated concept '{concept.Name}' has unsupported validation rule '{rule.Kind}' (requirement '{rule.RequirementId}').",
                    specification.Id);
            }
        }

        return null;
    }

    static string? InvalidStrategyFixture(SemanticApplicationContext context, SemanticSpecification specification)
    {
        if (specification.When is not { } action ||
            !context.Commands.TryGetValue(action.Command, out var command))
        {
            return null;
        }
        foreach (var fixture in action.GeneratedValues)
        {
            var property = command.Properties.SingleOrDefault(property => property.Id == fixture.TargetProperty && property.IsGenerated);
            if (property is null) continue;
            var concept = context.Concepts[property.Type.Target];
            if ((SemanticGenerationShapes.UsesConceptGeneration(context.Request.Model.Application) || !concept.Validations.IsEmpty) &&
                !SemanticGenerationShapes.Matches(concept, fixture.Value))
            {
                return $"Specification '{specification.Name}' cannot render: {SemanticGenerationShapes.FixtureMessage(property.Name)}";
            }
            if (SemanticRuleValues.GeneratedFailure(concept, property.Name, fixture.Value) is { } failure)
            {
                return $"Specification '{specification.Name}' cannot render: {failure}.";
            }
        }

        return null;
    }

    static string? UnpinnedRuleBearingConcept(SemanticApplicationContext context, SemanticSpecification specification)
    {
        if (specification.When is not { } action || !context.Commands.TryGetValue(action.Command, out var command)) return null;

        // Date-segment pattern generation is refused, so the current UTC year cannot affect admitted output.
        var property = UnpinnedGeneratedProperties(command, action).FirstOrDefault(property =>
            RuleConsistency.Check(context.Concepts[property.Type.Target]).Any(result => result.Status != RuleConsistencyStatus.Proven));

        return property is null ? null : $"Specification '{specification.Name}' needs a pinned generated value for '{property.Name}': concept rules may reject an unpinned draw.";
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
