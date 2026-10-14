// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticCratisAdmission.when_explaining_command_capabilities;

public class with_a_validation_implementation_body : given.a_command_model
{
    void Establish() => _command = _command with { CodeValidations = [new("validate-description")] };
    void Because() => Evaluate();

    [Fact] void should_name_the_body_and_received_at_and_pure_limitations_and_fix() => Messages.ShouldContainOnly(["Command 'IssueInvoice' validation implementation body 'validate-description' cannot be rendered: Arc does not supply RuleContext.Occurred (received-at) to a generated validator, and Stage does not yet enforce the pure capability. Use equivalent declarative validation; implementation bodies are not supported yet (Cratis/Stage#409)."]);
    [Fact] void should_keep_the_code() => _diagnostics.Single().Code.ShouldEqual("STAGE-ESM-005");
}
