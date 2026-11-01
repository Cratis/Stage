// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Runtime;
using Cratis.Stage.Semantics;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticFactAppender;

public class when_appending_a_published_public_event : given.a_translating_application
{
    IReadOnlyList<SemanticFact> _facts = [];

    void Establish()
    {
        var packed = _plan.Events.Values.Single(contract => contract.Name == "OrderPacked");
        var shipped = _plan.Events.Values.Single(contract => contract.Name == "OrderShipped");
        _facts =
        [
            new(packed.Id, SemanticValue.Text("order-1"), [new(packed.Properties.Single().Id, SemanticValue.Text("order-1"))]),
            new(shipped.Id, SemanticValue.Text("order-1"), [new(shipped.Properties.Single().Id, SemanticValue.Text("packed"))])
        ];
    }

    async Task Because() => await new SemanticFactAppender(_client, new StageEventStoreName("Compliance"), _plan).Append(_facts, new(DateTimeOffset.UtcNow, "caller", "Caller", "user"));

    [Fact] void should_append_twice() => _appends.Count.ShouldEqual(2);
    [Fact] void should_append_the_private_event_to_the_event_log_first() => _appends[0].EventSequenceId.ShouldEqual((string)EventSequenceId.Log);
    [Fact] void should_keep_the_public_event_out_of_the_event_log() => _appends[0].Events.Select(@event => @event.EventType.Id).ShouldContainOnly("OrderPacked");
    [Fact] void should_append_the_public_event_to_the_outbox() => _appends[1].EventSequenceId.ShouldEqual((string)EventSequenceId.Outbox);
    [Fact] void should_publish_only_the_public_event() => _appends[1].Events.Select(@event => @event.EventType.Id).ShouldContainOnly("OrderShipped");
}
