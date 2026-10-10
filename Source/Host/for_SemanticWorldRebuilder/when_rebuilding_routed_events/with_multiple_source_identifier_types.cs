// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Contracts.Queries;
using Cratis.Chronicle.Contracts.Sequences;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Host.for_SemanticRuntime.given;
using NSubstitute;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticWorldRebuilder.when_rebuilding_routed_events;

public class with_multiple_source_identifier_types : a_routed_runtime
{
    internal static string MultipleSourceModel => Source.Replace(
        "module Banking",
        """
        concept InvoiceId : Uuid
        eventsource Invoice
          identifier InvoiceId
          stream Changes
            streamId Period
        module Banking
        """,
        StringComparison.Ordinal) + """

            slice StateChange DepositInvoice
              command DepositInvoice
                invoiceId InvoiceId identifier
                period Period
                amount Int
                authorize Owner
                stream Invoice.Changes
                  streamId = period
                produces Deposited
                  for invoiceId
                  amount = amount
        """;

    readonly List<AppendedEventResponse> _stored = [];
    SemanticWorld _world = null!;
    ulong _tail = ulong.MaxValue;

    protected override string ModelSource => MultipleSourceModel;

    async Task Establish()
    {
        _contracts.Sequences.TailSequenceNumber(Arg.Any<TailSequenceNumberRequest>()).Returns(_ =>
            QueryResult<EventSequenceTailResponse>.Success(Guid.Empty, new() { SequenceNumber = _tail }));
        _contracts.Sequences.AppendManyForEventSources(Arg.Any<AppendManyForEventSourcesRequest>()).Returns(call =>
        {
            foreach (var appended in call.Arg<AppendManyForEventSourcesRequest>().Events)
            {
                _stored.Add(new()
                {
                    Content = appended.Content,
                    Context = new()
                    {
                        EventType = appended.EventType,
                        EventSourceType = appended.EventSourceType,
                        EventSourceId = appended.EventSourceId,
                        EventStreamType = appended.EventStreamType,
                        EventStreamId = appended.EventStreamId,
                        SequenceNumber = (ulong)_stored.Count,
                        Occurred = appended.Occurred,
                        Tags = appended.Tags ?? []
                    }
                });
            }
            _tail = (ulong)_stored.Count - 1;

            return Cratis.Chronicle.Contracts.Commands.CommandResult<AppendManyResponse>.Success(Guid.Empty, new() { IsSuccess = true });
        });
        foreach (var (commandName, identifier, value) in new[]
        {
            ("Deposit", "accountId", "acc-1"),
            ("DepositInvoice", "invoiceId", "3fa85f64-5717-4562-b3fc-2c963f66afa6")
        })
        {
            var command = _plan.Commands.Values.Single(candidate => candidate.Name == commandName);
            var result = await _runtime.Execute(
                command,
                new Dictionary<string, JsonElement>
                {
                    [identifier] = JsonSerializer.SerializeToElement(value),
                    ["period"] = JsonSerializer.SerializeToElement("2026-10"),
                    ["amount"] = JsonSerializer.SerializeToElement(10)
                },
                _principal,
                new(new DateTimeOffset(2026, 10, 1, 12, 0, 0, TimeSpan.Zero), "owner", "Owner", "owner"),
                false);
            result.ShouldBeOfExactType<SemanticAccepted>();
        }
    }

    void Because() => _world = SemanticWorldRebuilder.Create(_plan, _stored, _tail);

    [Fact] void should_append_and_rebuild_both_facts() => _world.Facts.Length.ShouldEqual(2);
    [Fact] void should_rebuild_the_same_event_contract() => _world.Facts.Select(fact => fact.EventContract).Distinct().Count().ShouldEqual(1);
    [Fact] void should_use_the_accounts_declared_identifier_type() => _world.Facts[0].Context!.EventSource.Type.ShouldEqual(IdentifierType("Account"));
    [Fact] void should_use_the_invoices_declared_identifier_type() => _world.Facts[1].Context!.EventSource.Type.ShouldEqual(IdentifierType("Invoice"));
    [Fact] void should_preserve_the_account_identity() => _world.Facts[0].Destination.ShouldEqual(SemanticValue.Text("acc-1"));
    [Fact] void should_preserve_the_invoice_identity() => _world.Facts[1].Destination.ShouldEqual(SemanticValue.Text("3fa85f64-5717-4562-b3fc-2c963f66afa6"));

    SemanticTypeReference IdentifierType(string source) => _plan.Model.Application.EventSources.Single(candidate => candidate.Name == source).IdentifierType!;
}
