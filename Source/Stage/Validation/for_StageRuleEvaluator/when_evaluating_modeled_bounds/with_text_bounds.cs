// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Validation.for_StageRuleEvaluator.when_evaluating_modeled_bounds;

public class with_text_bounds : given.a_modeled_rule
{
    bool[] _accepted = null!;

    void Because() => _accepted = [.. new (ValidationRuleKind Kind, string Value)[]
    {
        (ValidationRuleKind.Min, "\"9\""), (ValidationRuleKind.Min, "\"12\""), (ValidationRuleKind.Min, "\"123\""),
        (ValidationRuleKind.Max, "\"9\""), (ValidationRuleKind.Max, "\"12\""), (ValidationRuleKind.Max, "\"123\""),
        (ValidationRuleKind.Min, "\"\\uD83D\\uDE00\""), (ValidationRuleKind.Max, "\"\\uD83D\\uDE00\""),
        (ValidationRuleKind.Min, "null"), (ValidationRuleKind.Max, "null")
    }.Select(example => StageRuleEvaluator.Evaluate(Rules("name", example.Kind, 2d), $$"""{"name":{{example.Value}}}""").Count == 0)];

    [Fact] void should_measure_utf16_length_and_allow_null() => _accepted.ShouldEqual([false, true, true, true, true, false, true, true, true, true]);
}
