// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticCratisAdmission.when_explaining_command_capabilities;

public class with_an_unrenderable_validation : given.a_command_model
{
    void Establish() => _command = _command with { Validations = [new(_command.Properties.Single(property => property.Name == "description").Id, SemanticValidationRuleKind.NotEmpty, null, "Name {PropertyName} is required")] };
    void Because() => Evaluate();

    [Fact] void should_name_the_rule_property_and_message_fix() => Messages.ShouldContainOnly(["Command 'IssueInvoice' validation 'NotEmpty' on property 'description' cannot be rendered: the message contains formatting, control, or surrogate characters that FluentValidation cannot preserve. Use a literal message without these characters."]);
    [Fact] void should_keep_the_code() => _diagnostics.Single().Code.ShouldEqual("STAGE-ESM-005");
}
