// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticValidationRendering.when_rendering_not_empty;

public class with_a_collection : Specification
{
    CSharpCodeBuilder _builder = null!;
    SemanticValidationRule _rule = null!;

    void Establish()
    {
        _builder = new();
        _rule = invoice_model.Compile("module Names\n  feature Registration\n    slice StateChange Register\n      command Register\n        names String[]\n        validate\n          names not empty").Application.Modules.Single().Features.Single().Slices.Single().Commands.Single().Validations.Single();
    }

    void Because() => SemanticValidationRendering.Render(_builder, _rule, "names", SemanticPrimitiveType.Text, collection: true, concept: false);

    [Fact] void should_require_at_least_one_element() => _builder.ToString().ShouldContain("RuleFor(_ => _.Names).Must(value => value is { Count: > 0 }).WithMessage(\"A required value is empty.\");");
    [Fact] void should_not_render_the_builtin_not_empty_validator() => _builder.ToString().ShouldNotContain(".NotEmpty()");
    [Fact] void should_not_apply_text_whitespace_semantics_to_elements() => _builder.ToString().ShouldNotContain("IsNullOrWhiteSpace");
}
