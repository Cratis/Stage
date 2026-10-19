// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle;
using Cratis.Chronicle.Connections;
using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Contracts.EventStores;
using Cratis.Chronicle.Contracts.EventTypes;
using Cratis.Chronicle.Contracts.Queries;
using Cratis.Chronicle.Contracts.Sequences;
using Cratis.Screenplay;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Host.for_SemanticSeeds.given;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

using AppendResult = Cratis.Chronicle.Contracts.Commands.CommandResult<Cratis.Chronicle.Contracts.Sequences.AppendManyResponse>;
using CommandResult = Cratis.Chronicle.Contracts.Commands.CommandResult;

namespace Cratis.Stage.Host.for_SemanticChronicleRegistration.given;

public class a_seeding_store : a_seeded_model
{
    protected const string Store = "Seeded";

    protected IServices _contracts = null!;
    protected IChronicleClient _client = null!;
    internal SemanticSeedPlan _seeds = null!;
    protected readonly List<AppendManyForEventSourcesRequest> _appends = [];
    protected readonly HashSet<string> _sourcesWithEvents = [];

    protected void Prepare(string seeds, ulong tail = ulong.MaxValue)
    {
        Compile(seeds);
        _seeds = SemanticSeeds.Admit(SemanticSeeds.Create(Syntax.Seeds, _plan), _plan);
        _contracts = Substitute.For<IServices>();
        _contracts.EventStores.EnsureEventStore(Arg.Any<EnsureEventStoreRequest>()).Returns(CommandResult.Success(Guid.Empty));
        _contracts.EventTypes.RegisterEventTypes(Arg.Any<RegisterEventTypesRequest>()).Returns(CommandResult.Success(Guid.Empty));
        _contracts.Sequences.TailSequenceNumber(Arg.Any<TailSequenceNumberRequest>()).Returns(QueryResult<EventSequenceTailResponse>.Success(Guid.Empty, new() { SequenceNumber = tail }));
        _contracts.Sequences.HasEventsForEventSourceId(Arg.Any<HasEventsForEventSourceIdRequest>())
            .Returns(call => QueryResult<EventSourceEventsResponse>.Success(Guid.Empty, new() { HasEvents = _sourcesWithEvents.Contains(call.Arg<HasEventsForEventSourceIdRequest>().EventSourceId) }));
        _contracts.Sequences.AppendManyForEventSources(Arg.Do<AppendManyForEventSourcesRequest>(_appends.Add)).Returns(AppendResult.Success(Guid.Empty, new() { IsSuccess = true }));
        var connection = Substitute.For<IChronicleConnection, IChronicleServicesAccessor>();
        connection.Lifecycle.Returns(new ConnectionLifecycle(NullLogger<ConnectionLifecycle>.Instance));
        ((IChronicleServicesAccessor)connection).Services.Returns(_contracts);
        var store = Substitute.For<IEventStore>();
        store.Name.Returns(new EventStoreName(Store));
        store.Connection.Returns(connection);
        _client = Substitute.For<IChronicleClient>();
        _client.GetEventStore(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName?>()).Returns(store);
    }
}
