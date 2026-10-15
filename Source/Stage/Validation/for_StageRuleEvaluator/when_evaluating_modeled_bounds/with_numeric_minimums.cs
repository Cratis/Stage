// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Validation.for_StageRuleEvaluator.when_evaluating_modeled_bounds;

public class with_numeric_minimums : given.a_modeled_rule
{
    bool[] _accepted = null!;

    void Because() => _accepted = [.. new (double Bound, string Value)[]
    {
        (1, "0"), (1, "1"), (1, "12"), (10, "9"), (10, "10"), (10, "100"),
        (1.25, "1.24"), (1.25, "1.25"), (1.25, "12.5"), (-2.5, "-2.6"), (-2.5, "-2.5")
    }.Select(example => StageRuleEvaluator.Evaluate(Rules("amount", ValidationRuleKind.Min, example.Bound), $$"""{"amount":{{example.Value}}}""").Count == 0)];

    [Fact] void should_compare_values_instead_of_digit_counts() => _accepted.ShouldEqual([false, true, true, false, true, true, false, true, true, false, true]);
}
