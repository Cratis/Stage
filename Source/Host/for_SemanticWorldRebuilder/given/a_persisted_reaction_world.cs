// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Contracts.Sequences;
using Cratis.Chronicle.Events;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Host.for_SemanticRuntime.given;
using Cratis.Stage.Semantics;

namespace Cratis.Stage.Host.for_SemanticWorldRebuilder.given;

public class a_persisted_reaction_world : a_routed_runtime
{
    protected SemanticWorld _live = null!;
    protected AppendedEventResponse[] _history = [];
    protected SemanticWorld _rebuilt = null!;
    protected virtual bool Routed => false;
    protected virtual bool ExplicitDestination => true;

    protected override string ModelSource => Source
        .Replace("          amount = amount", "          amount = amount\n          accountId = accountId", StringComparison.Ordinal)
        .Replace("      event Deposited\n        amount Int", "      event Deposited\n        accountId AccountId\n        amount Int", StringComparison.Ordinal)
        .Replace("        stream Account.Transactions\n          streamId = period\n", Routed ? "        stream Account.Transactions\n          streamId = period\n" : string.Empty, StringComparison.Ordinal) + """

            slice Automation Completion
              reaction CompleteDeposit
                when Deposited
                  accountId
                  amount
                  produces Completed
                    for accountId
                    label = "done"
                    amount = amount
              event Completed
                tag completed
                label String
                amount Int
            slice StateView Summary
              readmodel DepositSummary
                accountId AccountId
                label String
                amount Int
              query SummaryById => DepositSummary?
                by accountId AccountId
              projection Summary => DepositSummary
                from Completed key $eventSourceId
                  label = label
                  amount = amount
        """.Replace("            for accountId\n", ExplicitDestination ? "            for accountId\n" : string.Empty, StringComparison.Ordinal);

    async Task Establish()
    {
        var accepted = (SemanticAccepted)await Execute();
        _live = accepted.World;
        _history = [.. _append!.Events.Select((@event, index) => new AppendedEventResponse
        {
            Context = new()
            {
                EventType = @event.EventType,
                EventSourceId = @event.EventSourceId,
                SequenceNumber = (ulong)index,
                EventSourceType = string.IsNullOrEmpty(@event.EventSourceType) ? EventSourceType.Default.Value : @event.EventSourceType,
                EventStreamType = string.IsNullOrEmpty(@event.EventStreamType) ? EventStreamType.All.Value : @event.EventStreamType,
                EventStreamId = string.IsNullOrEmpty(@event.EventStreamId) ? EventStreamId.Default : @event.EventStreamId,
                Occurred = @event.Occurred,
                Tags = @event.Tags,
                CausedBy = _append.CausedBy
            },
            Content = @event.Content
        })];
    }

    // ReactionOrigin is transient evaluator provenance, not persisted by the Chronicle fact appender.
    protected string DurableFacts(SemanticWorld world) => JsonSerializer.Serialize(world.Facts.Select(fact => new
    {
        EventContract = fact.EventContract.ToString(),
        Destination = SemanticJsonValues.ToObject(fact.Destination),
        SourceType = fact.Context?.EventSource.Type.ToString(),
        SourceValue = fact.Context is { } context ? SemanticJsonValues.ToObject(context.EventSource.Value) : null,
        fact.Tags,
        fact.Route,
        fact.Occurred,
        Values = fact.Values.OrderBy(value => value.TargetProperty.ToString(), StringComparer.Ordinal)
            .Select(value => new { TargetProperty = value.TargetProperty.ToString(), Value = SemanticJsonValues.ToObject(value.Value) })
    }));

    protected string ReadModels(SemanticWorld world) => JsonSerializer.Serialize(world.ReadModels.Select(model => new
    {
        ReadModel = model.ReadModel.ToString(),
        Key = SemanticJsonValues.ToObject(model.Key),
        Values = model.Values.OrderBy(value => value.TargetProperty.ToString(), StringComparer.Ordinal)
            .Select(value => new { TargetProperty = value.TargetProperty.ToString(), Value = SemanticJsonValues.ToObject(value.Value) })
    }));
}
