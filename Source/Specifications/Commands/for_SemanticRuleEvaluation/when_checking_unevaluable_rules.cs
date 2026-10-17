// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Stage.Rendering.Cratis.Semantics;

namespace Cratis.Stage.Specifications.Commands.for_SemanticRuleEvaluation;

public class when_checking_unevaluable_rules : given.a_not_empty_rule
{
    void Establish() => Prepare("\"name\"", concept: true);

    [Theory]
    [InlineData(SemanticValidationRuleKind.RulePredicate, null)]
    [InlineData(SemanticValidationRuleKind.CodeValidation, null)]
    [InlineData(SemanticValidationRuleKind.Unknown, null)]
    [InlineData(SemanticValidationRuleKind.Matches, "(?=a)a")]
    public void should_refuse_execution_and_generated_fixtures_even_for_absence(SemanticValidationRuleKind kind, string? pattern)
    {
        var rule = new SemanticValidationRule(default, kind, pattern is null ? null : SemanticValue.Text(pattern), null);
        var concept = _application.Concepts.Single() with { Validations = [rule] };
        SemanticValidationRules.Check(rule, SemanticValue.Null).Status.ShouldEqual(SemanticRuleStatus.NotEvaluable);
        Catch.Exception(() => SemanticRuleEvaluation.Satisfies(rule, SemanticValue.Null)).ShouldBeOfExactType<UnsupportedSemanticMapping>();
        Catch.Exception(() => SemanticGenerationShapes.GeneratedFailure(concept, "name", SemanticValue.Null)).ShouldBeOfExactType<InvalidSemanticContract>();
    }
}
