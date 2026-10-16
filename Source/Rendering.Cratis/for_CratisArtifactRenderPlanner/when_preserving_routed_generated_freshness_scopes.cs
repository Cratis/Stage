// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.Commands;
using Cratis.Arc.Commands;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Chronicle.EventSources;
using Cratis.Concepts;
using Cratis.Execution;
using Cratis.Specifications;
using NSubstitute;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_preserving_routed_generated_freshness_scopes : Specification
{
    readonly EventSourceId _source = new("11111111-1111-4111-8111-111111111111");
    IEventSources _sources = null!;
    IEventLog _log = null!;
    CommandContext _context = null!;
    ConcurrencyScope _expected = null!;
    ConcurrencyScope _actual = null!;
    EventForEventSourceId _appended = null!;

    async Task Establish()
    {
        _sources = Substitute.For<IEventSources>();
        _sources.GetFor(typeof(Account)).Returns(new EventSourceDefinition(typeof(Account), "account", "", ConcurrencyDimensions.EventSourceId | ConcurrencyDimensions.EventStreamType | ConcurrencyDimensions.EventStreamId, [new("transactions", "", ConcurrencyDimensions.None)]));
        var services = Substitute.For<IServiceProvider>();
        services.GetService(typeof(IEventSources)).Returns(_sources);
        var command = new RecordTransaction();
        _context = new(CorrelationId.New(), typeof(RecordTransaction), command, [], new EventSourceDefinitionValuesProvider(services).Provide(command), null);
        await new CommandHandlerArgumentResolver(new NoProvidedValues()).Resolve(new ModelBoundCommandHandler(typeof(RecordTransaction), typeof(RecordTransaction).GetMethod("Handle")!), _context, services, null);
        _expected = new(EventSequenceNumber.BeforeFirst, EventSourceId: _source);
        _log = Substitute.For<IEventLog>();
        _log.Id.Returns(EventSequenceId.Log);
        _log.AppendMany(Arg.Any<IEnumerable<EventForEventSourceId>>(), concurrencyScopes: Arg.Any<IDictionary<EventSourceId, ConcurrencyScope>>()).Returns(call =>
        {
            _appended = call.Arg<IEnumerable<EventForEventSourceId>>().Single();
            _actual = call.Arg<IDictionary<EventSourceId, ConcurrencyScope>>().Single().Value;
            return AppendManyResult.Success(_context.CorrelationId, [EventSequenceNumber.First]);
        });
    }

    async Task Because() => await new EventsWithConcurrencyScopesCommandResponseValueHandler(_log, _sources).Handle(_context, new EventsWithConcurrencyScopes(
        [new(_source, new Recorded()) { EventStreamId = new("new-stream") }], [new(_source, _expected)]));

    [Fact] void should_capture_the_same_unguarded_identifier_as_rendered_commands() => _context.GeneratedValues.Single().Freshness.ShouldEqual(GeneratedValueFreshness.Unguarded);
    [Fact] void should_use_the_definition_concurrency_dimensions() => _context.GetEventRoute().Concurrency.ShouldEqual(ConcurrencyDimensions.EventSourceId | ConcurrencyDimensions.EventStreamType | ConcurrencyDimensions.EventStreamId);
    [Fact] void should_preserve_the_explicit_whole_source_scope() => _actual.ShouldEqual(_expected);
    [Fact] void should_leave_the_stream_type_unrestricted() => _actual.EventStreamType.ShouldBeNull();
    [Fact] void should_leave_the_stream_id_unrestricted() => _actual.EventStreamId.ShouldBeNull();
    [Fact] void should_leave_the_source_type_unrestricted() => _actual.EventSourceType.ShouldBeNull();
    [Fact] void should_keep_routing_metadata_on_the_appended_event() => _appended.EventStreamType.ShouldEqual(new EventStreamType("transactions"));

    [global::Cratis.Arc.Chronicle.Commands.EventSource<Account>("transactions")]
    record RecordTransaction
    {
        public void Handle([GeneratedValue(Freshness = GeneratedValueFreshness.Unguarded)] Identifier receipt)
        {
        }
    }

    public record Identifier(Guid Value) : EventSourceId<Guid>(Value), IGeneratable<Identifier>
    {
        public static Identifier New() => new(new Guid("11111111-1111-4111-8111-111111111111"));
    }

    [global::Cratis.Chronicle.EventSources.EventSource]
    [EventStream("transactions")]
    class Account : IEventSource;

    record Recorded;

    sealed class NoProvidedValues : ICommandProvideInvoker
    {
        public ValueTask<IReadOnlyList<object>> Invoke(CommandContext context, IServiceProvider serviceProvider) => new([]);
    }
}
