// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Stage.Contracts;
using Cratis.Stage.Contracts.Specifications;
using Cratis.Stage.Validation;

namespace Cratis.Stage.Running;

/// <summary>
/// Shared helpers used by the <see cref="ISpecificationRunStrategy"/> implementations to verify the
/// structural consistency of a specification against its slice.
/// </summary>
public static class SpecificationVerification
{
    /// <summary>
    /// Builds a step that checks every Given event refers to an event that exists on the slice.
    /// </summary>
    /// <param name="slice">The slice the specification belongs to.</param>
    /// <param name="specification">The specification being verified.</param>
    /// <returns>The resulting <see cref="SpecificationRunStep"/>.</returns>
    public static SpecificationRunStep VerifyGivenEvents(Slice slice, Specification specification)
    {
        var differences = specification.Given
            .Where(given => !SliceHasEvent(slice, given.EventId, given.Name))
            .Select(given => new SpecificationValueDifference(given.Name, "an event on the slice", "not found"))
            .ToArray();

        return new SpecificationRunStep(
            SpecificationRunStepKind.Given,
            $"Given {specification.Given.Count} event(s)",
            differences.Length == 0 ? SpecificationRunOutcome.Passed : SpecificationRunOutcome.Failed,
            differences.Length == 0 ? null : "One or more Given events do not exist on the slice.",
            differences);
    }

    /// <summary>
    /// Builds a step that checks every Then event refers to an event that exists on the slice.
    /// </summary>
    /// <param name="slice">The slice the specification belongs to.</param>
    /// <param name="specification">The specification being verified.</param>
    /// <returns>The resulting <see cref="SpecificationRunStep"/>.</returns>
    public static SpecificationRunStep VerifyThenEvents(Slice slice, Specification specification)
    {
        var differences = specification.ThenEvents
            .Where(then => !SliceHasEvent(slice, then.EventId, then.Name))
            .Select(then => new SpecificationValueDifference(then.Name, "an event on the slice", "not found"))
            .ToArray();

        return new SpecificationRunStep(
            SpecificationRunStepKind.ThenEvents,
            $"Then {specification.ThenEvents.Count} event(s)",
            differences.Length == 0 ? SpecificationRunOutcome.Passed : SpecificationRunOutcome.Failed,
            differences.Length == 0 ? null : "One or more Then events do not exist on the slice.",
            differences);
    }

    /// <summary>
    /// Builds a step that checks the When command refers to the command defined on the slice.
    /// </summary>
    /// <param name="slice">The slice the specification belongs to.</param>
    /// <param name="specification">The specification being verified.</param>
    /// <returns>The resulting <see cref="SpecificationRunStep"/>.</returns>
    public static SpecificationRunStep VerifyWhenCommand(Slice slice, Specification specification)
    {
        if (specification.When is not { } when)
        {
            return new SpecificationRunStep(
                SpecificationRunStepKind.When,
                "When command",
                SpecificationRunOutcome.Failed,
                "No command is set for the specification.",
                []);
        }

        var resolves = slice.Command is { } command &&
            (command.Id == when.CommandId || string.Equals(command.Name, when.Name, StringComparison.Ordinal));

        return new SpecificationRunStep(
            SpecificationRunStepKind.When,
            $"When {when.Name}",
            resolves ? SpecificationRunOutcome.Passed : SpecificationRunOutcome.Failed,
            resolves ? null : "The When command does not match the command defined on the slice.",
            resolves ? [] : [new SpecificationValueDifference(when.Name, "the slice command", slice.Command?.Name ?? "none")]);
    }

    /// <summary>
    /// Builds a step that checks the modeled command <c>notEmpty</c>/<c>notNull</c> rules are consistent with
    /// whether the specification models a Then error for the supplied When values.
    /// </summary>
    /// <param name="slice">The slice the specification belongs to.</param>
    /// <param name="specification">The specification being verified.</param>
    /// <returns>The resulting <see cref="SpecificationRunStep"/>.</returns>
    public static SpecificationRunStep VerifyThenErrors(Slice slice, Specification specification)
    {
        var expectsError = specification.ThenErrors.Count > 0;
        var violatedProperties = PredictRuleViolations(slice, specification);
        var wouldFailValidation = violatedProperties.Count > 0;

        if (expectsError == wouldFailValidation)
        {
            return new SpecificationRunStep(
                SpecificationRunStepKind.ThenErrors,
                expectsError ? "Then error(s)" : "No Then errors",
                SpecificationRunOutcome.Passed,
                null,
                []);
        }

        var differences = wouldFailValidation
            ? violatedProperties.Select(property => new SpecificationValueDifference(property, "no validation error", "rule violated")).ToArray()
            : [new SpecificationValueDifference("Then errors", "a validation error", "no rule would reject the When values")];

        return new SpecificationRunStep(
            SpecificationRunStepKind.ThenErrors,
            "Then error(s)",
            SpecificationRunOutcome.Failed,
            wouldFailValidation
                ? "The When values violate the modeled rules but the specification models no Then error."
                : "The specification models a Then error but no modeled rule would reject the When values.",
            differences);
    }

    /// <summary>
    /// Aggregates step outcomes into an overall outcome: failed if any step failed, otherwise inconclusive
    /// if any step is inconclusive, otherwise passed.
    /// </summary>
    /// <param name="steps">The steps to aggregate.</param>
    /// <returns>The aggregated <see cref="SpecificationRunOutcome"/>.</returns>
    public static SpecificationRunOutcome Aggregate(IReadOnlyList<SpecificationRunStep> steps)
    {
        if (steps.Any(step => step.Outcome == SpecificationRunOutcome.Failed))
        {
            return SpecificationRunOutcome.Failed;
        }

        if (steps.Any(step => step.Outcome == SpecificationRunOutcome.Inconclusive))
        {
            return SpecificationRunOutcome.Inconclusive;
        }

        return SpecificationRunOutcome.Passed;
    }

    static List<string> PredictRuleViolations(Slice slice, Specification specification)
    {
        if (slice.Command is not { } command || specification.When is not { } when)
        {
            return [];
        }

        return [.. StageRuleEvaluator.Evaluate(command.Rules, when.Values)
            .Select(violation => violation.PropertyName)
            .Distinct()];
    }

    static bool SliceHasEvent(Slice slice, Guid eventId, string name) =>
        slice.Events.Any(@event => @event.Id == eventId || string.Equals(@event.Name, name, StringComparison.Ordinal));
}
