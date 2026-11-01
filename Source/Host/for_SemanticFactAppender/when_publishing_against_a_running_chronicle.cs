// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle;
using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Contracts.Queries;
using Cratis.Chronicle.Contracts.Sequences;
using Cratis.Chronicle.EventSequences;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Runtime;
using Cratis.Stage.Semantics;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticFactAppender;

// Runs only when STAGE_CHRONICLE_URL names a Chronicle (for example chronicle://localhost:35213); without it the spec has nothing to prove against and passes without running.
public class when_publishing_against_a_running_chronicle : given.a_translating_application
{
    readonly string _url = Environment.GetEnvironmentVariable("STAGE_CHRONICLE_URL") ?? string.Empty;
    ulong _logTail = ulong.MaxValue;
    ulong _outboxTail = ulong.MaxValue;
    bool _ran;

    async Task Because()
    {
        if (_url.Length == 0)
        {
            return;
        }

        var name = "Stage213" + Guid.NewGuid().ToString("N");
        using var client = new ChronicleClient(new ChronicleOptions(_url));
        await SemanticChronicleRegistration.Register(client, name, _plan);
        var packed = _plan.Events.Values.Single(contract => contract.Name == "OrderPacked");
        var shipped = _plan.Events.Values.Single(contract => contract.Name == "OrderShipped");
        await new SemanticFactAppender(client, new StageEventStoreName(name), _plan).Append(
            [
                new(packed.Id, SemanticValue.Text("order-1"), [new(packed.Properties.Single().Id, SemanticValue.Text("order-1"))]),
                new(shipped.Id, SemanticValue.Text("order-1"), [new(shipped.Properties.Single().Id, SemanticValue.Text("packed"))])
            ],
            new(DateTimeOffset.UtcNow, "caller", "Caller", "user"));
        var store = await client.GetEventStore(name);
        var accessor = (IChronicleServicesAccessor)store.Connection;
        _logTail = await Tail(accessor, name, EventSequenceId.Log);
        _outboxTail = await Tail(accessor, name, EventSequenceId.Outbox);
        _ran = true;
    }

    [Fact] void should_hold_the_private_event_in_the_event_log() => (!_ran || _logTail == 0).ShouldBeTrue();
    [Fact] void should_hold_the_public_event_in_the_outbox() => (!_ran || _outboxTail == 0).ShouldBeTrue();

    static async Task<ulong> Tail(IChronicleServicesAccessor accessor, string name, EventSequenceId sequence) =>
        (await accessor.Services.Sequences.TailSequenceNumber(new TailSequenceNumberRequest { EventStore = name, Namespace = EventStoreNamespaceName.Default, EventSequenceId = sequence })).EnsureSuccess().SequenceNumber;
}
