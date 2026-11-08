// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Validation;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rules;
using Cratis.Stage.Runtime;
using Xunit;

namespace Cratis.Stage.Api.for_StageCommandHandler.when_handling_a_command;

public class and_a_rule_on_a_nested_secret_property_does_not_hold : given.a_command_handler
{
    const string SecretValue = "s3cret";

    Exception? _error;

    async Task Because() => _error = await Catch.Exception(() => HandlerWith(
            "{\"type\":\"object\",\"properties\":{\"invoiceId\":{},\"credentials\":{\"type\":\"object\",\"properties\":{\"password\":{\"type\":\"string\",\"x-conceptAttributes\":{\"sensitive\":\"\"}}}}}}",
            new CommandPropertyRules("credentials.password", [new MinLength(20, "Password {PropertyValue} has {TotalLength} characters")]))
        .Handle(ContextFor($"{{\"invoiceId\":\"33333333-3333-3333-3333-333333333333\",\"credentials\":{{\"password\":\"{SecretValue}\"}}}}")).AsTask());

    [Fact] void should_reject_it_as_a_validation_failure() => _error.ShouldBeOfExactType<CommandInputRejected>();
    [Fact] void should_not_contain_the_secret_value() => _error.Message.ShouldNotContain(SecretValue);
    [Fact] void should_not_reveal_the_length() => _error.Message.ShouldNotContain("characters");
    [Fact] void should_use_a_generic_message() => _error.Message.ShouldContain("'credentials.password' is invalid.");
}
