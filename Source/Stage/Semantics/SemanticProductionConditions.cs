// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Stage.Rendering.Cratis.Semantics;

namespace Cratis.Stage.Semantics;

internal static class SemanticProductionConditions
{
    internal static IEnumerable<SemanticProducedEvent> Taken(SemanticCommand command, SemanticSpecificationCommand action)
    {
        if (command.Produces.All(produced => produced.When is null)) return command.Produces;
        var values = action.Values.Concat(action.GeneratedValues).ToDictionary(value => value.TargetProperty, value => value.Value);

        // Missing generation is refused at execution after input validation and route checks.
        // Until then, keep all possible reachable events rather than guessing a random condition.
        if (command.Properties.Any(property => property.IsGenerated && !values.ContainsKey(property.Id))) return command.Produces;
        return command.Produces.Where(produced => produced.When is null || Holds(produced.When, values));
    }

    static bool Holds(SemanticCondition condition, IReadOnlyDictionary<SemanticId, SemanticValue> values) => condition switch
    {
        SemanticLogicalCondition { Operator: SemanticLogicalOperator.And } logical => Holds(logical.Left, values) && Holds(logical.Right, values),
        SemanticLogicalCondition { Operator: SemanticLogicalOperator.Or } logical => Holds(logical.Left, values) || Holds(logical.Right, values),
        SemanticComparison comparison => Compare(comparison, values),
        _ => throw new InvalidSemanticContract("Unknown condition node or operator.")
    };

    static bool Compare(SemanticComparison comparison, IReadOnlyDictionary<SemanticId, SemanticValue> values) =>
        SemanticProductionComparison.Compare(
            comparison,
            values,
            (() => new InvalidSemanticContract("Ordering condition requires numeric operands."), () => new InvalidSemanticContract("Unknown comparison operator.")));
}
