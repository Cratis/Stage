// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Host.for_SemanticRuntime.given;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticRuntime.when_executing;

public class and_the_command_is_routed : a_routed_runtime
{
    SemanticExecutionResult _result = null!;

    async Task Because() => _result = await Execute();

    [Fact] void should_accept_the_command() => _result.ShouldBeOfExactType<SemanticAccepted>();
    [Fact] void should_send_the_source_definition() => _append!.Events.Single().EventSource.ShouldEqual("stored-account");
    [Fact] void should_send_the_source_type() => _append!.Events.Single().EventSourceType.ShouldEqual("stored-account");
    [Fact] void should_send_the_stream_type() => _append!.Events.Single().EventStreamType.ShouldEqual("stored-transactions");
    [Fact] void should_send_the_formatted_stream_id() => _append!.Events.Single().EventStreamId.ShouldEqual("2026-10");
    [Fact] void should_preserve_the_destination() => _append!.Events.Single().EventSourceId.ShouldEqual("acc-1");
}
