// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticCratisAdmission.when_explaining_command_capabilities;

public class with_a_validation_implementation_body : given.a_command_model
{
    void Establish() => _command = _command with { CodeValidations = [new("validate-description")] };
    void Because() => Evaluate();

    [Fact] void should_name_the_unresolved_body() => _diagnostics.Single().Message.ShouldContain("validate-description");
    [Fact] void should_require_the_implementation_envelope() => _diagnostics.Single().Code.ShouldEqual("STAGE-ESM-020");
}
