// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticValidationRendering.when_rendering_not_empty;

public class with_a_custom_message_and_severity : Specification
{
    CSharpCodeBuilder _builder = null!;
    SemanticValidationRule _rule = null!;

    void Establish()
    {
        _builder = new();
        _rule = new(default, SemanticValidationRuleKind.NotEmpty, null, "A name is required.") { Severity = SemanticValidationSeverity.Warning };
    }

    void Because() => SemanticValidationRendering.Render(_builder, _rule, "name", SemanticPrimitiveType.Text, collection: false, concept: false);

    [Fact] void should_keep_the_message_and_severity_on_the_builtin_validator() => _builder.ToString().ShouldContain("RuleFor(_ => _.Name).NotEmpty().WithMessage(\"A name is required.\").WithSeverity(global::Cratis.Arc.Validation.ValidationResultSeverity.Warning);");
}
