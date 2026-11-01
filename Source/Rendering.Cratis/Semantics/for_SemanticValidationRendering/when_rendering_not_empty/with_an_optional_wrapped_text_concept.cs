// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticValidationRendering.when_rendering_not_empty;

public class with_an_optional_wrapped_text_concept : Specification
{
    CSharpCodeBuilder _builder = null!;
    SemanticValidationRule _rule = null!;

    void Establish()
    {
        _builder = new();
        _rule = invoice_model.Compile("concept Name : String\nmodule Names\n  feature Registration\n    slice StateChange Register\n      command Register\n        name Name optional\n        validate\n          name not empty").Application.Modules.Single().Features.Single().Slices.Single().Commands.Single().Validations.Single();
    }

    void Because() => SemanticValidationRendering.Render(_builder, _rule, "name", SemanticPrimitiveType.Text, collection: false, concept: false, wrapped: true, optional: true);

    [Fact] void should_keep_the_existing_whitespace_predicate() => _builder.ToString().ShouldContain("RuleFor(_ => _.Name).Must(value => !global::System.String.IsNullOrWhiteSpace(value)).WithMessage(\"A required value is empty.\");");
    [Fact] void should_not_render_the_builtin_not_empty_validator() => _builder.ToString().ShouldNotContain(".NotEmpty()");
}
