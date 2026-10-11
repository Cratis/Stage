// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Validation;
using Cratis.Chronicle.EventSequences;
using Cratis.Specifications;
using OneOf;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticStateChangeArtifactRenderer.when_routing_a_command;

public class with_a_large_integer_stream_id : given.a_routed_command
{
    EventForEventSourceId _result = null!;

    void Because()
    {
        Plan();
        var assembly = Load();
        var account = Activator.CreateInstance(assembly.GetType("InvoiceApp.Common.AccountId")!, Guid.NewGuid())!;
        var month = Activator.CreateInstance(assembly.GetType("InvoiceApp.Common.Month")!, 9007199254740991L)!;
        var commandType = assembly.GetType("InvoiceApp.Banking.Deposits.Deposit.Deposit")!;
        var command = Activator.CreateInstance(commandType, account, month, 42m)!;
        _result = ((OneOf<EventForEventSourceId, ValidationResult>)commandType.GetMethod("Handle")!.Invoke(command, null)!).AsT0;
    }

    [Fact] void should_route_the_full_safe_integer_value() => _result.EventStreamId.Value.ShouldEqual("9007199254740991");
}
