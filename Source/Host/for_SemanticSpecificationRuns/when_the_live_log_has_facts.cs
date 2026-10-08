// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Specifications.Semantic;
using Cratis.Stage.Host.for_SemanticSpecificationRuns.given;
using NSubstitute;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticSpecificationRuns;

public class when_the_live_log_has_facts : a_specification_endpoint
{
    IEventLog _hostLog = null!;
    EventSequenceNumber _tail = null!;

    void Establish()
    {
        _hostLog = _hostEventStore.EventLog;
        _hostLog.GetTailSequenceNumber().Returns(new EventSequenceNumber(7));
        _hostEventStore.ClearReceivedCalls();
        _hostLog.ClearReceivedCalls();
    }

    async Task Because()
    {
        await Request("{}");
        _tail = await _hostLog.GetTailSequenceNumber();
    }

    [Fact] void should_execute_against_isolated_state() => Report.Results.Single().Outcome.ShouldEqual(SemanticSpecificationOutcome.Passed);
    [Fact] void should_not_access_the_host_event_store() => _hostEventStore.ReceivedCalls().ShouldBeEmpty();
    [Fact] void should_not_append_to_the_host_event_log() => _hostLog.ReceivedCalls().Select(call => call.GetMethodInfo().Name).ShouldContainOnly(nameof(IEventLog.GetTailSequenceNumber));
    [Fact] void should_preserve_the_live_tail() => _tail.ShouldEqual(new EventSequenceNumber(7));
}
