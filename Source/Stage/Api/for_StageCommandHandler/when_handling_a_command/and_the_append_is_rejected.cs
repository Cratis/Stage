// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Runtime;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace Cratis.Stage.Api.for_StageCommandHandler.when_handling_a_command;

public class and_the_append_is_rejected : given.a_command_handler
{
    Exception? _error;

    void Establish() => _appender.Append(Arg.Any<string>(), Arg.Any<IReadOnlyList<ProducedEventPayload>>(), Arg.Any<IReadOnlyDictionary<string, string>>())
        .ThrowsAsync(new ProducedEventAppendRejected("Append rejected"));

    async Task Because() => _error = await Catch.Exception(() => HandlerFor("invoiceId").Handle(ContextFor("{\"invoiceId\":\"33333333-3333-3333-3333-333333333333\"}")).AsTask());

    [Fact] void should_fail_instead_of_returning_the_payload() => _error.ShouldBeOfExactType<ProducedEventAppendRejected>();
    [Fact] void should_report_the_reason() => _error!.Message.ShouldEqual("Append rejected");
}
