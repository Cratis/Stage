// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticValidationRendering.when_rendering_not_empty;

public class with_a_concept_value : Specification
{
    CSharpCodeBuilder _builder = null!;
    SemanticValidationRule _rule = null!;

    void Establish()
    {
        _builder = new();
        _rule = invoice_model.Compile("concept Name : String\n  validate\n    not empty").Application.Concepts.Single().Validations.Single();
    }

    void Because() => SemanticValidationRendering.Render(_builder, _rule, "Value", SemanticPrimitiveType.Text, collection: false, concept: true);

    [Fact] void should_validate_the_primitive_value_with_the_builtin_validator() => _builder.ToString().ShouldContain("RuleFor(_ => _.Value).NotEmpty().WithMessage(\"A required concept value is empty.\");");
    [Fact] void should_not_render_a_custom_predicate() => _builder.ToString().ShouldNotContain(".Must(");
}
