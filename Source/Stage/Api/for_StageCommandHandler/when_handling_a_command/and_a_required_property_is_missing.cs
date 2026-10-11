// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Validation;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rules;
using Cratis.Stage.Runtime;
using NSubstitute;
using Xunit;

namespace Cratis.Stage.Api.for_StageCommandHandler.when_handling_a_command;

public class and_a_required_property_is_missing : given.a_command_handler
{
    Exception? _error;

    async Task Because() => _error = await Catch.Exception(() => HandlerWith("{\"type\":\"object\",\"properties\":{\"invoiceId\":{},\"authorName\":{}},\"required\":[\"invoiceId\",\"authorName\"]}")
        .Handle(ContextFor("{\"invoiceId\":\"33333333-3333-3333-3333-333333333333\"}")).AsTask());

    [Fact] void should_reject_it_as_a_validation_failure() => _error.ShouldBeOfExactType<CommandInputRejected>();
    [Fact] void should_name_the_missing_property() => ((CommandInputRejected)_error!).ValidationResult.Members.ShouldContainOnly("authorName");
    [Fact] void should_append_nothing() => _appender.DidNotReceiveWithAnyArgs().Append(default!, default!, default!);
}
