// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.for_StateChangeSliceRenderer.given;
using Cratis.Stage.Rendering.Cratis.Renderers;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_StateChangeSliceRenderer;

public class when_rendering_a_command_with_validation_rules : state_change_slices
{
    RenderedFile _file = null!;
    StateChangeSliceRenderer _renderer = null!;

    void Establish() => _renderer = new StateChangeSliceRenderer();

    void Because() => _file = _renderer.Render(_registerInvoice, _applicationSet, "CratisApp");

    [Fact] void should_declare_a_paired_validator() =>
        _file.Content.ShouldContain("public class RegisterInvoiceValidator : CommandValidator<RegisterInvoice>");
    [Fact] void should_emit_the_rule_against_the_pascal_case_property() =>
        _file.Content.ShouldContain("RuleFor(_ => _.Name).NotEmpty().WithMessage(\"Name is required\");");
}
