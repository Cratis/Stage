// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticValidationRendering;

public class when_rendering_an_optional_boolean_concept_command_rule : Specification
{
    CSharpCodeBuilder _builder = null!;
    SemanticValidationRule _rule = null!;

    void Establish()
    {
        _builder = new();
        _rule = invoice_model.Compile("concept Flag : Bool\nmodule Inputs\n  feature Optional\n    slice StateChange Check\n      command Check\n        value Flag optional\n        validate\n          value == false").Application.Modules.Single().Features.Single().Slices.Single().Commands.Single().Validations.Single();
    }

    void Because() => SemanticValidationRendering.Render(_builder, _rule, "value", SemanticPrimitiveType.Boolean, collection: false, concept: false, wrapped: true, optional: true, conceptRendering: new("global::Inputs.Common.Flag?"));

    [Fact] void should_select_the_nullable_concept_property_without_implicit_unwrapping() => _builder.ToString().ShouldContain("RuleFor<global::Inputs.Common.Flag?>(_ => _.Value)");
    [Fact] void should_compare_the_unwrapped_boolean_and_accept_absence() => _builder.ToString().ShouldContain("value is null || global::System.Object.Equals(value.Value, false)");
    [Fact] void should_not_compare_the_concept_object_to_a_boolean() => _builder.ToString().ShouldNotContain("Object.Equals(value, false)");
}
