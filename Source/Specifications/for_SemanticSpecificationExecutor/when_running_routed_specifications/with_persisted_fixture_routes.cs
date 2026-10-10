// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Testing;
using Cratis.Chronicle.Testing.EventSequences;
using Cratis.Screenplay.Semantics;
using Cratis.Stage.Api;
using Cratis.Stage.Specifications.Commands;
using Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.given;
using Cratis.Stage.Specifications.Types;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.when_running_routed_specifications;

public class with_persisted_fixture_routes : a_routed_plan
{
    AppendedEvent[] _persisted = [];

    async Task Because()
    {
        var types = new SemanticRuntimeTypes(_plan);
        using var scenario = new EventScenario(new Defaults(new SemanticClientArtifactsProvider([.. _plan.Events.Keys.Select(types.For)])));
        var context = new SemanticRunContext(typeof(DynamicCommand), _command, _specification, new(), types, scenario.EventLog, _plan);
        var fixture = _specification.ThenEvents[0] with { EventSource = new(Text, SemanticValue.Text("acc-1")) };
        await context.Append(fixture, CancellationToken.None);
        await context.Append(fixture with { Route = null }, CancellationToken.None);
        context.TryResolveRoute(out _).ShouldBeTrue();
        var (fact, destination) = context.Produce(_command.Produces[0]);
        await context.Append(fact, destination, CancellationToken.None);
        _persisted = [.. await scenario.EventLog.GetFromSequenceNumber(EventSequenceNumber.First)];
    }

    [Fact] void should_persist_the_fixture_source_type() => _persisted[0].Context.EventSourceType.Value.ShouldEqual("stored-account");
    [Fact] void should_persist_the_fixture_stream_type() => _persisted[0].Context.EventStreamType.Value.ShouldEqual("stored-transactions");
    [Fact] void should_persist_the_fixture_stream_id() => _persisted[0].Context.EventStreamId.Value.ShouldEqual("2026-10");
    [Fact] void should_keep_the_default_source_for_an_unrouted_fixture() => _persisted[1].Context.EventSourceType.Value.ShouldEqual(EventSourceType.Default.Value);
    [Fact] void should_keep_the_default_stream_type_for_an_unrouted_fixture() => _persisted[1].Context.EventStreamType.Value.ShouldEqual(EventStreamType.All.Value);
    [Fact] void should_keep_the_default_stream_id_for_an_unrouted_fixture() => _persisted[1].Context.EventStreamId.Value.ShouldEqual(EventStreamId.Default);
    [Fact] void should_persist_the_produced_source_type() => _persisted[2].Context.EventSourceType.Value.ShouldEqual("stored-account");
    [Fact] void should_persist_the_produced_stream_type() => _persisted[2].Context.EventStreamType.Value.ShouldEqual("stored-transactions");
    [Fact] void should_persist_the_produced_stream_id() => _persisted[2].Context.EventStreamId.Value.ShouldEqual("2026-10");
}
