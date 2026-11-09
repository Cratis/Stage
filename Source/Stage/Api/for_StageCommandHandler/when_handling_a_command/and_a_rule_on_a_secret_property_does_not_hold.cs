// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Validation;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rules;
using Cratis.Stage.Runtime;
using Xunit;

namespace Cratis.Stage.Api.for_StageCommandHandler.when_handling_a_command;

public class and_a_rule_on_a_secret_property_does_not_hold : given.a_command_handler
{
    const string SecretValue = "s3cret";

    Exception? _error;

    async Task Because() => _error = await Catch.Exception(() => HandlerWith(
            "{\"type\":\"object\",\"properties\":{\"invoiceId\":{},\"password\":{\"type\":\"string\",\"x-conceptAttributes\":{\"sensitive\":\"\"}},\"note\":{\"type\":\"string\"}}}",
            new CommandPropertyRules("password", [new MinLength(20, "Password {PropertyValue} has {TotalLength} characters")]),
            new CommandPropertyRules("note", [new MinLength(20, "Note {PropertyValue} is too short")]))
        .Handle(ContextFor($"{{\"invoiceId\":\"33333333-3333-3333-3333-333333333333\",\"password\":\"{SecretValue}\",\"note\":\"short\"}}")).AsTask());

    [Fact] void should_reject_it_as_a_validation_failure() => _error.ShouldBeOfExactType<CommandInputRejected>();
    [Fact] void should_not_contain_the_secret_value() => _error.Message.ShouldNotContain(SecretValue);
    [Fact] void should_not_reveal_the_length() => _error.Message.ShouldNotContain("characters");
    [Fact] void should_use_a_generic_message_for_the_secret_property() => _error.Message.ShouldContain("'password' is invalid.");
    [Fact] void should_still_report_the_modeled_message_of_other_properties() => _error.Message.ShouldContain("Note short is too short");
}
