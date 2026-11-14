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
using Cratis.Screenplay.Syntax;
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

    /// <summary>Gets the first-generation schemas the kernel already holds, by stored event name; an event type not in it is not registered yet.</summary>
    protected readonly Dictionary<string, string> _storedSchemas = [];

    /// <summary>Prepares the store with the seeds; an exact price replaces every seeded price, since a legacy literal reaches the model as a double.</summary>
    /// <param name="seeds">The seed source.</param>
    /// <param name="tail">The tail sequence number the store reports.</param>
    /// <param name="exactPrice">The exact decimal every seeded price becomes, if any.</param>
    protected void Prepare(string seeds, ulong tail = ulong.MaxValue, string? exactPrice = null)
    {
        Compile(seeds);
        _seeds = SemanticSeeds.Admit(SemanticSeeds.Create(exactPrice is null ? Syntax.Seeds : WithExactPrice(Syntax.Seeds, exactPrice), _plan), _plan);
        _contracts = Substitute.For<IServices>();
        _contracts.EventStores.EnsureEventStore(Arg.Any<EnsureEventStoreRequest>()).Returns(CommandResult.Success(Guid.Empty));
        _contracts.EventTypes.RegisterEventTypes(Arg.Do<RegisterEventTypesRequest>(request =>
        {
            // The kernel keeps the shape the first registration gave an event type.
            foreach (var type in request.Types)
            {
                _storedSchemas.TryAdd(type.Type.Id, type.Schema);
            }
        })).Returns(CommandResult.Success(Guid.Empty));
        _contracts.EventTypes.AllEventTypeGenerations(Arg.Any<AllEventTypeGenerationsRequest>()).Returns(call =>
            QueryResult<IEnumerable<EventTypeDetailsResponse>>.Success(Guid.Empty, [.. _storedSchemas
                .Where(entry => entry.Key == call.Arg<AllEventTypeGenerationsRequest>().EventTypeId)
                .Select(entry => new EventTypeDetailsResponse { Id = entry.Key, Type = new() { Id = entry.Key, Generation = 1 }, Schema = entry.Value })]));
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

    static SeedSyntax[] WithExactPrice(IEnumerable<SeedSyntax> seeds, string price)
    {
        if (!ExactNumber.TryParse(price, out var exact))
        {
            throw new ArgumentException($"'{price}' is not an exact number.", nameof(price));
        }

        PropertyMappingSyntax Exact(PropertyMappingSyntax mapping) =>
            mapping.Property == "price" ? new(mapping.Property, new LiteralExpressionSyntax(exact, mapping.Location), mapping.Location) : mapping;
        return [.. seeds.Select(seed => new SeedSyntax(
            [.. seed.Groups.Select(group => new SeedGroupSyntax(
                group.EventSourceId,
                [.. group.Events.Select(@event => new SeedEventSyntax(@event.Event, [.. @event.Properties.Select(Exact)], @event.Location))],
                group.Location))],
            seed.Location))];
    }
}
