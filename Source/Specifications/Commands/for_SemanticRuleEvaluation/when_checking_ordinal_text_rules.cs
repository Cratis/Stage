// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

namespace Cratis.Stage.Specifications.Commands.for_SemanticRuleEvaluation;

public class when_checking_ordinal_text_rules : Specification
{
    [Theory]
    [InlineData(SemanticValidationRuleKind.Contains)]
    [InlineData(SemanticValidationRuleKind.NotContains)]
    [InlineData(SemanticValidationRuleKind.StartsWith)]
    [InlineData(SemanticValidationRuleKind.EndsWith)]
    public void should_preserve_reference_outcomes_and_default_messages(SemanticValidationRuleKind kind)
    {
        SemanticRuleValues.CanEvaluate(kind).ShouldBeTrue();
        foreach (var operand in new[] { "ABC", "", ".*", "İ", "é" })
        {
            var rule = new SemanticValidationRule(default, kind, SemanticValue.Text(operand), null);
            foreach (var input in new[] { "ABC", "abc", "xABCx", "ABCx", "xABC", "", null, ".*", "AB\0C", "ABC\n", "İ", "i", "é", "e\u0301" })
            {
                var value = input is null ? SemanticValue.Null : SemanticValue.Text(input);
                var reference = SemanticValidationRules.Check(rule, value);
                SemanticRuleEvaluation.Satisfies(rule, value).ShouldEqual(reference.Status == SemanticRuleStatus.Satisfied);
                SemanticRuleEvaluation.Message(rule, value, "unused").ShouldEqual(reference.Message);
            }
        }
    }
}
