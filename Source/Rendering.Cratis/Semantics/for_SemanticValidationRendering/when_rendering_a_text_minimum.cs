// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticValidationRendering;

public class when_rendering_a_text_minimum : Specification
{
    CSharpCodeBuilder _builder = null!;
    SemanticValidationRule _rule = null!;

    void Establish()
    {
        _builder = new();
        _rule = invoice_model.Compile("module Names\n  feature Registration\n    slice StateChange Register\n      command Register\n        name String\n        validate\n          name min 2").Application.Modules.Single().Features.Single().Slices.Single().Commands.Single().Validations.Single();
    }

    void Because() => SemanticValidationRendering.Render(_builder, _rule, "name", SemanticPrimitiveType.Text, collection: false, concept: false);

    [Fact] void should_keep_the_minimum_length_predicate_and_absence_guard() => _builder.ToString().ShouldContain(".Must(value => value is null || value.Length >= 2m)");
    [Fact] void should_keep_the_default_message() => _builder.ToString().ShouldContain(".WithMessage(\"A value must be at least 2 characters long.\");");
    [Fact] void should_not_render_the_builtin_not_empty_validator() => _builder.ToString().ShouldNotContain(".NotEmpty()");
}
