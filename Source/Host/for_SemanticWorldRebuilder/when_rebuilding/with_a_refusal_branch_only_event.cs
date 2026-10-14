// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Contracts.Sequences;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Host.for_SemanticRuntime.given;
using Cratis.Stage.Semantics;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticWorldRebuilder.when_rebuilding;

public class with_a_refusal_branch_only_event : Specification
{
    SemanticExecutionPlan _plan = null!;
    SemanticWorld _live = null!;
    SemanticWorld _rebuilt = null!;
    AppendedEventResponse[] _history = [];

    void Establish()
    {
        _plan = compiled_plan.From("""
            module Work
              feature Processing
                slice Automation Handling
                  command Begin
                    id String identifier
                    produces Started
                      for id
                      id = id
                  command Reject
                    id String identifier
                    validate
                      id not empty message "required"
                  event Started
                    id String
                  event Completed
                    tag completed
                    message String
                  reaction Worker
                    when Started
                      invokes Reject
                        id = ""
                        on refused by validation
                          produces Completed
                            message = $refusal.message
                  specification Handled
                    when Begin
                      id = "one"
                    then Started
                      id = "one"
                    then Completed
                      message = "required"
            """);
        var run = new SemanticSpecificationRunner().Run(_plan, _plan.Specifications.Values.Single().Id);
        Assert.True(run.Passed, string.Join(';', run.Failures));
        _live = run.Execution.World;
        _history = [.. _live.Facts.Select((fact, index) => new AppendedEventResponse
        {
            Context = new()
            {
                EventType = new() { Id = _plan.Events[fact.EventContract].Name, Generation = 1 },
                EventSourceId = ((SemanticTextValue)fact.Destination).Value,
                SequenceNumber = (ulong)index,
                EventSourceType = "Default",
                EventStreamType = "All",
                EventStreamId = "Default",
                Occurred = new() { Value = "2026-10-01T12:00:00.0000000+00:00" },
                Tags = fact.Tags
            },
            Content = JsonSerializer.Serialize(fact.Values.ToDictionary(
                value => _plan.Events[fact.EventContract].Properties.Single(property => property.Id == value.TargetProperty).Name,
                value => SemanticJsonValues.ToObject(value.Value)))
        })];
    }

    void Because() => _rebuilt = SemanticWorldRebuilder.Create(_plan, _history, 1);

    [Fact] void should_restore_the_branch_only_contract() => _rebuilt.Facts[1].EventContract.ShouldEqual(_live.Facts[1].EventContract);
    [Fact] void should_infer_the_destination_type_through_the_trigger() => _rebuilt.Facts[1].Context!.EventSource.Type.ShouldEqual(_live.Facts[1].Context!.EventSource.Type);
    [Fact] void should_restore_the_inherited_destination() => _rebuilt.Facts[1].Destination.ShouldEqual(SemanticValue.Text("one"));
    [Fact] void should_restore_the_refusal_payload() => _rebuilt.Facts[1].Values.Single().Value.ShouldEqual(SemanticValue.Text("required"));
    [Fact] void should_preserve_the_branch_event_tags() => _rebuilt.Facts[1].Tags.ShouldContain("completed");
    [Fact] void should_not_react_to_history_again() => _rebuilt.Facts.Length.ShouldEqual(2);
}
