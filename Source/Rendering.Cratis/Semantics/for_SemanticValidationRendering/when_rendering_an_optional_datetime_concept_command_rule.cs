// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticValidationRendering;

public class when_rendering_an_optional_datetime_concept_command_rule : Specification
{
    CSharpCodeBuilder _builder = null!;
    SemanticValidationRule _rule = null!;

    void Establish()
    {
        _builder = new();
        _rule = invoice_model.Compile("concept Moment : DateTime\nmodule Inputs\n  feature Optional\n    slice StateChange Check\n      command Check\n        value Moment optional\n        validate\n          value >= \"2026-01-01T00:00:00Z\"").Application.Modules.Single().Features.Single().Slices.Single().Commands.Single().Validations.Single();
    }

    void Because() => SemanticValidationRendering.Render(_builder, _rule, "value", SemanticPrimitiveType.DateTime, collection: false, concept: false, wrapped: true, optional: true, conceptRendering: new("global::Inputs.Common.Moment?"));

    [Fact] void should_select_the_nullable_nominal_property() => _builder.ToString().ShouldContain("RuleFor<global::Inputs.Common.Moment?>(_ => _.Value)");
    [Fact] void should_allow_absence_and_compare_the_unwrapped_datetime() => _builder.ToString().ShouldContain("value is null || value.Value.CompareTo(");
    [Fact] void should_not_use_nullable_struct_value_semantics() => _builder.ToString().ShouldNotContain("value.Value.Value");
}
