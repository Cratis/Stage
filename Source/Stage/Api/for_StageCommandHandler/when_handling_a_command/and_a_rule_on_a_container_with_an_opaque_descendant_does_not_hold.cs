// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Rules;
using Cratis.Stage.Runtime;
using Xunit;

namespace Cratis.Stage.Api.for_StageCommandHandler.when_handling_a_command;

public class and_a_rule_on_a_container_with_an_opaque_descendant_does_not_hold : given.a_command_handler
{
    Exception? _error;

    async Task Because() => _error = await Catch.Exception(() => HandlerWith(
            """
            {"type":"object","properties":{"invoiceId":{"type":"string"},"wrapper":{"type":"object","properties":{"credentials":{"type":"object"}}}}}
            """,
            new CommandPropertyRules("wrapper", [new MinLength(200, "Value {PropertyValue} has {TotalLength} characters")]))
        .Handle(ContextFor("""
            {"invoiceId":"33333333-3333-3333-3333-333333333333","wrapper":{"credentials":{"password":"opaque-test-sentinel"}}}
            """)).AsTask());

    [Fact] void should_reject_it_as_a_validation_failure() => _error.ShouldBeOfExactType<CommandInputRejected>();
    [Fact] void should_not_include_the_opaque_value() => _error.Message.ShouldNotContain("opaque-test-sentinel");
    [Fact] void should_not_reveal_the_length() => _error.Message.ShouldNotContain("characters");
    [Fact] void should_use_a_value_free_message() => _error.Message.ShouldContain("'wrapper' is invalid.");
}
