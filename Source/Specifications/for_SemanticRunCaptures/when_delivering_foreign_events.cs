// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Testing.Events;
using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Screenplay.Semantics.Serialization;
using Cratis.Stage.Api;
using Cratis.Stage.Specifications.Commands;
using Cratis.Stage.Specifications.Types;

namespace Cratis.Stage.Specifications.for_SemanticRunCaptures;

public class when_delivering_foreign_events : Specification
{
    [Theory]
    [InlineData("FirstArrival", 2)]
    [InlineData("GivenTransition", 3)]
    [InlineData("Overlay", 3)]
    public async Task should_match_the_frozen_capture_world(string name, int expected)
    {
        var model = SemanticModelSerializer.Deserialize(PublicEventsCorpus.CapturesFromEventsV9.EsmBytes.AsSpan());
        var plan = SemanticExecutionPlan.Compile(model).Plan!;
        var specification = plan.Specifications.Values.Single(candidate => candidate.Name == name);
        var types = new SemanticRuntimeTypes(plan);
        using var store = new EventStoreForTesting(null, new SemanticClientArtifactsProvider([.. plan.Events.Keys.Select(types.For)]));
        var context = new SemanticRunContext(typeof(DynamicCommand), null!, specification, new(), types, store.EventLog, plan);
        foreach (var given in specification.GivenEvents) await context.Append(given, CancellationToken.None);
        var captures = new SemanticRunCaptures(plan, context);
        captures.Seed(specification.GivenEvents.Length).ShouldBeNull();
        var appended = specification.WhenAppended!;
        var fact = new SemanticSpecificationEvent(appended.EventContract, appended.Values) { EventSource = appended.EventSource, Route = appended.Route };
        await context.Append(fact, appended.EventSource?.Value ?? SemanticValue.Null, CancellationToken.None);
        var failure = await captures.Deliver(specification.GivenEvents.Length, _ => Task.FromResult<SemanticRunReactionFailure?>(null), CancellationToken.None);
        failure.ShouldBeNull();
        context.Facts.Count.ShouldEqual(expected);
        var local = context.Facts.Skip(specification.GivenEvents.Length + 1).ToArray();
        local.Length.ShouldEqual(1);
        var expectedName = name switch
        {
            "FirstArrival" => "Added",
            "Overlay" => "LabelRecorded",
            _ => "StatusRecorded"
        };
        plan.Events[local.Single().EventContract].Name.ShouldEqual(expectedName);

        // Re-observing the same record neither changes state nor re-fires an added guard.
        await context.Append(fact, appended.EventSource?.Value ?? SemanticValue.Null, CancellationToken.None);
        (await captures.Deliver(context.Facts.Count - 1, _ => Task.FromResult<SemanticRunReactionFailure?>(null), CancellationToken.None)).ShouldBeNull();
        context.Facts.Count.ShouldEqual(expected + 1);
    }
}
