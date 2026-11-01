// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
namespace Cratis.Stage.Conformance.Specs.for_ThreeWayConformance.given;

internal static class routed_generated_freshness_probe
{
    internal const string Source = """
        using Cratis.Arc.Chronicle.Testing.Commands;
        using Cratis.Arc.Commands;
        using Cratis.Arc.Testing.Commands;
        using Cratis.Arc.Validation;
        using Cratis.Chronicle.Events;
        using Cratis.Chronicle.EventSequences;
        using Cratis.Chronicle.EventSources;
        using Cratis.Specifications;
        using Microsoft.Extensions.DependencyInjection;
        using NSubstitute;
        using RoutedGeneration.Banking.Recording.Text;
        using RoutedGeneration.Banking.Recording.Uuid;
        using RoutedGeneration.Common;
        using RoutedGeneration.EventSources;
        using Xunit;

        namespace RoutedGeneration.Freshness;

        public class when_reusing_a_generated_source_across_streams : Specification, IDisposable
        {
            readonly CommandScenario<RecordUuid> _scenario = new();
            readonly Receipt _receipt = new(Guid.Parse("11111111-1111-7111-8111-111111111111"));
            EventsWithConcurrencyScopes _batch = null!;
            EventsWithConcurrencyScopes _checkedBatch = null!;
            AppendedEvent _prior = null!;
            AppendManyResult _direct = null!;
            CommandResult _result = null!;

            async Task Establish()
            {
                var sources = Substitute.For<IEventSources>();
                sources.GetFor(typeof(AccountEventSource)).Returns(new EventSourceDefinition(typeof(AccountEventSource), "Account", "Account",
                    ConcurrencyDimensions.EventSourceId | ConcurrencyDimensions.EventStreamType | ConcurrencyDimensions.EventStreamId,
                    [new(AccountEventSource.UuidStream, "", ConcurrencyDimensions.None), new(AccountEventSource.TextStream, "", ConcurrencyDimensions.None)]));
                _scenario.Services.AddSingleton(sources);
                _scenario.PinGeneratedValue("receipt", _receipt);
                var seeded = await _scenario.EventLog.Append(_receipt, new UuidRecorded(), eventSourceType: "Account", eventStreamType: AccountEventSource.TextStream, eventStreamId: "another-stream");
                seeded.IsSuccess.ShouldBeTrue();
                _prior = (await _scenario.EventLog.GetFromSequenceNumber(EventSequenceNumber.First)).Single();
                _batch = new RecordUuid(Guid.Parse("22222222-2222-4222-8222-222222222222")).Handle(_receipt);
                _checkedBatch = new RecordText("a|b%").Handle(_receipt).AsT0;
            }

            async Task Because()
            {
                _direct = await _scenario.EventLog.AppendMany(_batch.Events, concurrencyScopes: _batch.ConcurrencyScopes.ToDictionary(pair => pair.Key, pair => pair.Value));
                _result = await _scenario.Execute(new RecordUuid(Guid.Parse("22222222-2222-4222-8222-222222222222")));
            }

            [Fact] void should_use_the_stored_source_identity_as_the_scope_key() => _batch.ConcurrencyScopes.Single().Key.ShouldEqual(_prior.Context.EventSourceId);
            [Fact] void should_use_the_stored_source_identity_in_the_scope() => _batch.ConcurrencyScopes.Single().Value.EventSourceId.ShouldEqual(_prior.Context.EventSourceId);
            [Fact] void should_keep_scopes_through_the_oneof_wrapper() => _checkedBatch.ConcurrencyScopes.Single().Value.ShouldEqual(_batch.ConcurrencyScopes.Single().Value);
            [Fact] void should_reject_the_direct_scoped_append() => _direct.HasConcurrencyViolations.ShouldBeTrue();
            [Fact] void should_reject_the_command_with_definition_concurrency_dimensions() => _result.ValidationResults.Single().Reason.ShouldEqual(ValidationResultReason.ConcurrencyViolation);
            // With #660 read protection enabled, Execute captures command attempts rather than the earlier seed append.
            [Fact] void should_append_nothing_after_the_given() => _scenario.AppendedEvents.Count(entry => entry.Result.IsSuccess).ShouldEqual(0);
            [Fact]
            async Task should_preserve_only_the_given_on_its_original_stream()
            {
                var stored = (await _scenario.EventLog.GetFromSequenceNumber(EventSequenceNumber.First)).Single();
                stored.Content.ShouldEqual(_prior.Content);
                stored.Context.Hash.ShouldEqual(_prior.Context.Hash);
                stored.Context.SequenceNumber.ShouldEqual(_prior.Context.SequenceNumber);
                stored.Context.EventSourceId.ShouldEqual(_prior.Context.EventSourceId);
                stored.Context.EventStreamType.ShouldEqual(_prior.Context.EventStreamType);
                stored.Context.EventStreamId.ShouldEqual(_prior.Context.EventStreamId);
            }

            public void Dispose() => _scenario.Dispose();
        }
        """;
}
#endif
