// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Specifications.Commands.for_SemanticValueShapeAdmission;

public class when_refusing_nested_concept_rules : given.a_value_shape
{
    [Theory]
    [InlineData(SemanticValidationRuleKind.RulePredicate)]
    [InlineData(SemanticValidationRuleKind.CodeValidation)]
    [InlineData(SemanticValidationRuleKind.Unknown)]
    [InlineData((SemanticValidationRuleKind)999)]
    public void should_refuse_opaque_or_unevaluable_nested_rules(SemanticValidationRuleKind kind)
    {
        var concepts = (Dictionary<SemanticId, SemanticConcept>)_context.Concepts;
        var concept = concepts.Values.Single();
        concepts[concept.Id] = concept with { Validations = [concept.Validations.Single() with { Kind = kind }] };
        SemanticValueShapeAdmission.Refusal(_context, _type).ShouldEqual("A concept validation rule is not admitted.");
    }
}
