// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Testing.Events;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Api;
using Cratis.Stage.Contracts.Specifications.Semantic;
using Cratis.Stage.Specifications.Commands;
using Cratis.Stage.Specifications.Types;

namespace Cratis.Stage.Specifications.for_SemanticRunCaptures;

public class when_validating_source_keyed_delivery : Specification
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task should_reject_the_actual_source_before_mutating_capture_state(bool history)
    {
        var plan = when_running_source_keyed_captures.Plan(specification => specification);
        var specification = plan.Specifications.Values.Single();
        var types = new SemanticRuntimeTypes(plan);
        using var store = new EventStoreForTesting(null, new SemanticClientArtifactsProvider([.. plan.Events.Keys.Select(types.For)]));
        var context = new SemanticRunContext(typeof(DynamicCommand), null!, specification, new(), types, store.EventLog, plan);
        var appended = specification.WhenAppended!;
        var fact = new SemanticSpecificationEvent(appended.EventContract, appended.Values);
        await context.Append(fact, SemanticValue.Text("wrong"), CancellationToken.None);
        var captures = new SemanticRunCaptures(plan, context);
        var failure = history ? captures.Seed(1) : await captures.Deliver(0, Settle, CancellationToken.None);
        failure.ShouldNotBeNull();
        context.Facts.Count.ShouldEqual(1);
        var record = failure!.ToRecord(plan.Model.Application.Modules.Single().Features.Single().Slices.Single(slice => slice.Kind == SemanticSliceKind.Translate), specification);
        record.Trace!.Rejection.ShouldContain("from source 'wrong'");

        await context.Append(fact, appended.Values.Single().Value, CancellationToken.None);
        (await captures.Deliver(1, Settle, CancellationToken.None)).ShouldBeNull();
        context.Facts.Count.ShouldEqual(3);
        context.Destinations[^1].ShouldEqual(appended.Values.Single().Value);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task should_preserve_the_empty_key_message_when_seeding_or_delivering(bool history, bool missing)
    {
        var plan = when_running_source_keyed_captures.Plan(specification => specification, SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.Text));
        var specification = plan.Specifications.Values.Single();
        var types = new SemanticRuntimeTypes(plan);
        using var store = new EventStoreForTesting(null, new SemanticClientArtifactsProvider([.. plan.Events.Keys.Select(types.For)]));
        var context = new SemanticRunContext(typeof(DynamicCommand), null!, specification, new(), types, store.EventLog, plan);
        var appended = specification.WhenAppended!;
        var fact = new SemanticSpecificationEvent(appended.EventContract, missing ? [] : [new(appended.Values.Single().TargetProperty, SemanticValue.Null)]);
        var foreign = new SemanticFact(fact.EventContract, SemanticValue.Text("source"), fact.Values);
        var expected = Assert.Throws<InvalidSemanticContract>(() => new SemanticReactionSettlement(new SemanticEvaluator()).Settle(plan, SemanticWorld.Create([foreign], []), [foreign], null));
        await context.Append(fact, foreign.Destination, CancellationToken.None);
        var captures = new SemanticRunCaptures(plan, context);
        var failure = history ? captures.Seed(1) : await captures.Deliver(0, Settle, CancellationToken.None);
        var record = failure!.ToRecord(plan.Model.Application.Modules.Single().Features.Single().Slices.Single(), specification);
        record.Trace!.Rejection.ShouldEqual(expected.Message);
        context.Facts.Count.ShouldEqual(1);
    }

    [Fact]
    public async Task should_infer_numeric_keys_as_invariant_text_sources_and_keep_typed_output()
    {
        var type = SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.WholeNumber);
        var key = SemanticValue.Number(42);
        var plan = when_running_source_keyed_captures.Plan(
            specification => specification with
            {
                WhenAppended = specification.WhenAppended! with { Values = [new(specification.WhenAppended.Values.Single().TargetProperty, key)] },
                ThenEvents = [specification.ThenEvents.Single() with { Values = [new(specification.ThenEvents.Single().Values.Single().TargetProperty, key)], EventSource = new(type, key) }]
            },
            type);
        var specification = plan.Specifications.Values.Single();
        var normalized = SemanticCaptureSources.Normalize(plan, specification);
        normalized.WhenAppended!.EventSource!.Value.ShouldEqual(SemanticValue.Text("42"));
        new SemanticSpecificationRunner().Run(plan, specification.Id).Passed.ShouldBeTrue();
        var record = Assert.Single((await new SemanticSpecificationExecutor().Run(plan, new([]), new())).Results);
        Assert.True(record.Outcome == SemanticSpecificationOutcome.Passed, $"{record.Unsupported?.Details} {string.Join(';', record.Failures)}");
    }

    [Fact]
    public void should_not_infer_or_prevalidate_a_fixture_with_an_explicit_route()
    {
        var plan = when_running_source_keyed_captures.Plan(specification => specification);
        var appended = plan.Specifications.Values.Single().WhenAppended!;
        var routed = new SemanticSpecificationEvent(appended.EventContract, appended.Values)
        {
            Route = new(appended.EventContract, appended.EventContract),
            EventSource = new(SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.Text), SemanticValue.Text("wrong"))
        };
        SemanticCaptureSources.Normalize(plan, routed).ShouldEqual(routed);
        SemanticCaptureSources.Normalize(plan, routed with { EventSource = null }).EventSource.ShouldBeNull();
    }

    static Task<SemanticRunReactionFailure?> Settle(CancellationToken _) => Task.FromResult<SemanticRunReactionFailure?>(null);
}
