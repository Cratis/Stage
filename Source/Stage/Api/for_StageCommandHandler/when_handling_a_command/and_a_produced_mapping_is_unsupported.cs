// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Commands;
using Cratis.Stage.Runtime;
using NSubstitute;
using Xunit;

namespace Cratis.Stage.Api.for_StageCommandHandler.when_handling_a_command;

public class and_a_produced_mapping_is_unsupported : given.a_command_handler
{
    Exception? _error;

    async Task Because() => _error = await Catch.Exception(() => HandlerProducing(new ProducedEvent(
        "InvoiceRegistered",
        null,
        [new ProducedEventProperty("registeredByOn", ProducedValueKind.Unsupported, "causedBy.unrecognized")],
        [])).Handle(ContextFor("{\"invoiceId\":\"33333333-3333-3333-3333-333333333333\"}")).AsTask());

    [Fact] void should_fail_with_the_unresolvable_path() => _error!.Message.ShouldContain("$context.causedBy.unrecognized");
    [Fact] void should_not_append_an_incomplete_event() => _appender.DidNotReceive().Append(Arg.Any<string>(), Arg.Any<IReadOnlyList<ProducedEventPayload>>(), Arg.Any<IReadOnlyDictionary<string, string>>());
}
