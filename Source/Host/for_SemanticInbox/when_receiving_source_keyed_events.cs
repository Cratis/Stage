// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reactors;
using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Screenplay.Semantics.Serialization;
using Cratis.Specifications;
using Cratis.Stage.Semantics;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticInbox;

public class when_receiving_source_keyed_events : Specification
{
    const string First = "3fa85f64-5717-4562-b3fc-2c963f66afa6";
    const string Second = "9fa85f64-5717-4562-b3fc-2c963f66afa9";

    [Fact]
    public async Task should_preserve_the_explicit_inbox_source_and_suppress_identical_redelivery_per_key()
    {
        var plan = Plan();
        var log = new CapturedFacts(plan);
        using var runtime = new SemanticRuntime(plan, log);
        var first = Fact(plan, First, First);
        first.Destination.ShouldEqual(SemanticValue.Text(First));
        first.Context!.EventSource.Type.ShouldEqual(SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.Text));
        await runtime.Receive(first, Occurrence());
        await runtime.Receive(Fact(plan, Second, Second), Occurrence());
        await runtime.Receive(first, Occurrence());
        log.Facts.Count.ShouldEqual(2);
        log.Facts.Select(fact => fact.Destination).ShouldEqual([SemanticValue.Text(First), SemanticValue.Text(Second)]);
        log.ExpectedTails.ShouldEqual([ulong.MaxValue, 0UL, 1UL]);
    }

    [Fact]
    public async Task should_reject_an_explicit_mismatch_before_persisting_effects()
    {
        var plan = Plan();
        var log = new CapturedFacts(plan);
        using var runtime = new SemanticRuntime(plan, log);
        var foreign = Fact(plan, First, Second);
        var expected = Assert.Throws<InvalidSemanticContract>(() => new SemanticReactionSettlement(new SemanticEvaluator()).Settle(plan, SemanticWorld.Create([foreign], []), [foreign], null));
        var exception = await Assert.ThrowsAsync<InvalidSemanticContract>(() => runtime.Receive(foreign, Occurrence()));
        exception.Message.ShouldEqual(expected.Message);
        log.Facts.ShouldBeEmpty();
        await runtime.Receive(Fact(plan, First, First), Occurrence());
        log.Facts.Count.ShouldEqual(1);
    }

    static SemanticExecutionPlan Plan() => SemanticExecutionPlan.Compile(SemanticModelSerializer.Deserialize(EsmGoldenVectors.Get(33).Bytes.AsSpan())).Plan!;

    static SemanticFact Fact(SemanticExecutionPlan plan, string source, string key)
    {
        var contract = plan.Events.Values.Single(contract => contract.Origin is not null);
        var context = EventContext.Empty with { EventSourceId = new(source), EventType = new(SemanticStoredEventNames.Of(contract), EventTypeGeneration.First) };
        var received = new ReactorEvent(context, new JsonObject { [contract.Properties.Single().Name] = key }, new Dictionary<int, string>());
        return SemanticInbox.Fact(plan, contract.Origin!, received);
    }

    static SemanticCommandOccurrence Occurrence() => new(DateTimeOffset.UnixEpoch, "caller", "Caller", "user");

    sealed class CapturedFacts(SemanticExecutionPlan plan) : IAppendSemanticFacts, IAppendSemanticFactsAtTail, ISemanticFactTail
    {
        internal List<SemanticFact> Facts { get; } = [];
        internal List<ulong> ExpectedTails { get; } = [];
        ulong CurrentTail => Facts.Count == 0 ? ulong.MaxValue : (ulong)Facts.Count - 1;

        public Task<ulong> Tail() => Task.FromResult(CurrentTail);

        public Task Append(IReadOnlyList<SemanticFact> facts, SemanticCommandOccurrence occurrence) => Append(facts, occurrence, CurrentTail);

        public Task Append(IReadOnlyList<SemanticFact> facts, SemanticCommandOccurrence occurrence, ulong expectedTail)
        {
            ExpectedTails.Add(expectedTail);
            if (expectedTail != CurrentTail)
            {
                throw new SemanticFactTailChanged(CurrentTail);
            }

            Facts.AddRange(facts.Where(fact => plan.Events[fact.EventContract].Origin is null));
            return Task.CompletedTask;
        }
    }
}
