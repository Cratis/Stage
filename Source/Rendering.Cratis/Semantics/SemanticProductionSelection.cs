// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Semantics;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

/// <summary>
/// Selects command productions with the reference evaluator's scalar condition semantics.
/// </summary>
internal static class SemanticProductionSelection
{
    internal static ImmutableArray<SemanticProducedEvent> Taken(SemanticApplication application, SemanticCommand command, SemanticSpecification specification) =>
        Taken(application, command, specification.When!.Values.Concat(specification.When.GeneratedValues), SemanticSpecificationClock.TryGet(specification, out var instant) ? instant : null);

    internal static ImmutableArray<SemanticProducedEvent> Taken(SemanticApplication application, SemanticCommand command, IEnumerable<SemanticPropertyValue> values, DateTimeOffset? clock = null)
    {
        if (command.Produces.All(produced => produced.When is null) || command.Produces.Any(produced => produced.When is { } when && ReadsView(when))) return command.Produces;

        // Selection precedes admission. Keep every clock branch until an authored instant and supported shape are available.
        if (ClockRequiresAdmission(application, command, clock)) return command.Produces;

        // An offset-less DateTime input is rejected before any condition runs; selection keeps every production, as the Host does.
        var supplied = values.ToArray();
        if (SemanticDateComparisons.CheckInputs(application, command, supplied) is not null) return command.Produces;
        var inputs = supplied.ToDictionary(value => value.TargetProperty, value => value.Value);
        var dateTypes = SemanticDateComparisons.Participants(application, command);

        return [.. command.Produces.Where(produced => produced.When is null || Holds(produced.When, inputs, (dateTypes, clock)))];
    }

    static bool ClockRequiresAdmission(SemanticApplication application, SemanticCommand command, DateTimeOffset? clock) =>
        command.Produces.Any(produced => produced.When is { } when && SemanticCommandClockSupport.Uses(when)) &&
        (clock is null || !SemanticCommandClockSupport.Supports(application, command));

    // A condition over a protected read is refused by admission, which reports it; selection must not fail before that.
    internal static bool ReadsView(SemanticCondition condition) => condition switch
    {
        SemanticLogicalCondition logical => ReadsView(logical.Left) || ReadsView(logical.Right),
        SemanticComparison comparison => comparison.Left.Read is not null || comparison.Right.Read is not null,
        _ => false
    };

    internal static IEnumerable<SemanticId> Properties(SemanticCondition condition) => condition switch
    {
        SemanticLogicalCondition logical => Properties(logical.Left).Concat(Properties(logical.Right)),
        SemanticComparison comparison => new[] { comparison.Left, comparison.Right }.Where(operand => operand.Clock is null && operand.Property.IsSet).Select(operand => operand.Property),
        _ => throw UnsupportedSemanticRendering.For(nameof(SemanticCondition), condition.GetType().Name)
    };

    static bool Holds(SemanticCondition condition, IReadOnlyDictionary<SemanticId, SemanticValue> values, (IReadOnlyDictionary<SemanticId, SemanticPrimitiveType> Dates, DateTimeOffset? Clock) inputs) => condition switch
    {
        SemanticLogicalCondition { Operator: SemanticLogicalOperator.And } logical => Holds(logical.Left, values, inputs) && Holds(logical.Right, values, inputs),
        SemanticLogicalCondition { Operator: SemanticLogicalOperator.Or } logical => Holds(logical.Left, values, inputs) || Holds(logical.Right, values, inputs),
        SemanticComparison comparison => Compare(comparison, values, inputs),
        _ => throw UnsupportedSemanticRendering.For(nameof(SemanticCondition), condition.GetType().Name)
    };

    // A view is not an input of the command specification; admission refuses the condition, so selection takes the production.
    static bool Compare(SemanticComparison comparison, IReadOnlyDictionary<SemanticId, SemanticValue> values, (IReadOnlyDictionary<SemanticId, SemanticPrimitiveType> Dates, DateTimeOffset? Clock) inputs) =>
        comparison.Left.Read is not null || comparison.Right.Read is not null || SemanticProductionComparison.Compare(
            comparison,
            values,
            (() => UnsupportedSemanticRendering.For(nameof(SemanticComparison), comparison.Operator), () => UnsupportedSemanticRendering.For(nameof(SemanticComparisonOperator), comparison.Operator)),
            inputs.Dates,
            inputs.Clock);
}
