// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.Renderers;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Validation.for_ValidationRuleRenderer;

public class when_an_unknown_rule_escapes_admission : Specification
{
    Exception _error = null!;

    void Because() => _error = Catch.Exception(() => ValidationRuleRenderer.RenderCall((ValidationRuleKind)999, ""));

    [Fact] void should_throw_the_typed_internal_contract_refusal() => _error.ShouldBeOfExactType<UnsupportedValidationRule>();
    [Fact] void should_name_the_unknown_kind() => _error.Message.ShouldContain("999");
    [Fact] void should_use_the_unsupported_validation_code() => _error.Message.ShouldContain("STAGE-ESM-005");
}
