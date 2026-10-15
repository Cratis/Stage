// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Validation.for_StageRuleEvaluator.when_evaluating_modeled_bounds;

public class with_nested_or_absent_values : given.a_modeled_rule
{
    bool[] _accepted = null!;

    void Because() => _accepted = [.. new[]
    {
        """{"order":{"amount":12.5}}""",
        """{"order":{"amount":1.24}}""",
        """{"order":{"amount":null}}""",
        """{"order":{}}""",
        "{}"
    }.Select(json => StageRuleEvaluator.Evaluate(Rules("order.amount", ValidationRuleKind.Min, 1.25), json).Count == 0)];

    [Fact] void should_resolve_nested_numbers_and_allow_absence() => _accepted.ShouldEqual([true, false, true, true, true]);
}
