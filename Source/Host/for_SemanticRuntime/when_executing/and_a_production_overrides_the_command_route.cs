// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Host.for_SemanticRuntime.given;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticRuntime.when_executing;

public class and_a_production_overrides_the_command_route : a_routed_runtime
{
    SemanticExecutionResult _result = null!;

    protected override string ModelSource => """
        concept AccountId : String
        concept Period : String
        policy Owner
          require authenticated
        eventsource Account
          id "stored-account"
          identifier AccountId
          stream Transactions
            id "stored-transactions"
            streamId Period
          stream Archive
            id "stored-archive"
            streamId Period
        module Banking
          feature Deposits
            slice StateChange Deposit
              command Deposit
                accountId AccountId identifier
                period Period
                amount Int
                authorize Owner
                stream Account.Transactions
                  streamId = period
                produces Deposited
                  for accountId
                  amount = amount
                produces Archived
                  for accountId
                  amount = amount
                  stream Account.Archive
                    streamId = "archive-1"
              event Deposited
                amount Int
              event Archived
                amount Int
        """;

    async Task Because() => _result = await Execute();

    [Fact] void should_accept_the_command() => _result.ShouldBeOfExactType<SemanticAccepted>();
    [Fact] void should_send_both_events() => _append!.Events.Count().ShouldEqual(2);
    [Fact] void should_keep_the_command_stream_type_on_the_default_production() => _append!.Events.ElementAt(0).EventStreamType.ShouldEqual("stored-transactions");
    [Fact] void should_keep_the_command_stream_id_on_the_default_production() => _append!.Events.ElementAt(0).EventStreamId.ShouldEqual("2026-10");
    [Fact] void should_send_the_override_stream_type() => _append!.Events.ElementAt(1).EventStreamType.ShouldEqual("stored-archive");
    [Fact] void should_send_the_override_stream_id() => _append!.Events.ElementAt(1).EventStreamId.ShouldEqual("archive-1");
    [Fact] void should_preserve_the_destination_of_the_override() => _append!.Events.ElementAt(1).EventSourceId.ShouldEqual("acc-1");
}
