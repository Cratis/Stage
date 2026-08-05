// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.for_ConceptRenderer.given;
using Cratis.Stage.Rendering.Cratis.Renderers;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_ConceptRenderer;

public class when_rendering_a_validated_concept : concepts
{
    RenderedFile _file = null!;

    void Because() => _file = ConceptRenderer.Render(_discountPercentage, _applicationSet, "CratisApp");

    [Fact] void should_emit_a_validator_class() =>
        _file.Content.ShouldContain("public class DiscountPercentageValidator : ConceptValidator<DiscountPercentage>");
    [Fact] void should_emit_the_min_rule() =>
        _file.Content.ShouldContain("RuleFor(_ => _.Value).GreaterThanOrEqualTo(0).WithMessage(\"A discount cannot be negative\");");
    [Fact] void should_emit_the_max_rule() =>
        _file.Content.ShouldContain("RuleFor(_ => _.Value).LessThanOrEqualTo(100).WithMessage(\"A discount cannot exceed 100 percent\");");
    [Fact] void should_reference_the_custom_rule_by_must() =>
        _file.Content.ShouldContain("RuleFor(_ => _.Value).Must(SatisfyCustomRule).WithMessage(\"Must be a round number\");");
    [Fact] void should_emit_a_custom_rule_predicate_method() => _file.Content.ShouldContain("static bool SatisfyCustomRule(decimal Value)");
    [Fact] void should_embed_the_custom_rule_code() => _file.Content.ShouldContain("return Value % 1 == 0;");
}
