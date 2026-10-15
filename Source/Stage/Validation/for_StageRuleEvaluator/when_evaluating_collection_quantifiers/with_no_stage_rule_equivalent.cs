// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Validation.for_StageRuleEvaluator.when_evaluating_collection_quantifiers;

public class with_no_stage_rule_equivalent : given.a_modeled_rule
{
    bool[] _accepted = null!;

    void Because() => _accepted = [.. new[] { ValidationRuleKind.AllGreaterThan, ValidationRuleKind.AllGreaterThanOrEqual }
        .SelectMany(kind => new[]
        {
            """{"lines":[{"quantity":2},{"quantity":0}]}""",
            """{"lines":[{"quantity":2},{"quantity":3}]}""",
            """{"lines":[]}""",
            """{"lines":{"quantity":0}}"""
        }.Select(json => StageRuleEvaluator.Evaluate(Rules("lines.quantity", kind, 1d), json).Count == 0))];

    [Fact] void should_drop_both_quantifiers_for_every_payload_instead_of_half_enforcing_them() => _accepted.ShouldEqual([true, true, true, true, true, true, true, true]);
}
