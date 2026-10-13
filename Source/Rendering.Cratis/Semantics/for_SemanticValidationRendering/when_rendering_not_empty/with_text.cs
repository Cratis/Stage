// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticValidationRendering.when_rendering_not_empty;

public class with_text : Specification
{
    CSharpCodeBuilder _builder = null!;
    SemanticValidationRule _rule = null!;

    void Establish()
    {
        _builder = new();
        _rule = invoice_model.Compile("module Names\n  feature Registration\n    slice StateChange Register\n      command Register\n        name String\n        validate\n          name not empty").Application.Modules.Single().Features.Single().Slices.Single().Commands.Single().Validations.Single();
    }

    void Because() => SemanticValidationRendering.Render(_builder, _rule, "name", SemanticPrimitiveType.Text, collection: false, concept: false);

    [Fact] void should_render_unicode_whitespace_rejection() => _builder.ToString().ShouldContain("!global::System.String.IsNullOrWhiteSpace(value)");
    [Fact] void should_not_render_exact_empty_only_rejection() => _builder.ToString().ShouldNotContain("IsNullOrEmpty");
}
