// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Sequences;
using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Cratis.Stage.Host.for_SemanticRuntime.given;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticWorldRebuilder.when_rebuilding;

public class with_omitted_composite_members : Specification
{
    SemanticCompositeValue _details = null!;
    string[] _names = [];

    void Because()
    {
        var plan = compiled_plan.From("""
            concept Label : String
              validate
                not empty
            type Nested
              label Label optional
            type Details
              first Label optional
              second Label optional
              child Nested
            module Facts
              feature Recording
                slice StateChange Record
                  command RecordDetails
                    id String identifier
                    details Details
                    produces Recorded
                      for id
                      details = details
                  event Recorded
                    details Details
            """);
        var stored = new AppendedEventResponse
        {
            Content = """{"details":{"second":"two","child":{}}}""",
            Context = new()
            {
                EventType = new() { Id = plan.Events.Values.Single().Name, Generation = 1 },
                EventSourceId = "one",
                SequenceNumber = 0,
                EventSourceType = "Default",
                EventStreamType = "All",
                EventStreamId = "Default",
                Occurred = new() { Value = "2026-09-24T12:00:00.0000000+00:00" }
            }
        };
        var world = SemanticWorldRebuilder.Create(plan, [stored], 0);
        _details = Assert.IsType<SemanticCompositeValue>(world.Facts.Single().Values.Single().Value);
        var definition = plan.Model.Application.Types.Single(type => type.Name == "Details");
        _names = [.. _details.Properties.Select(member => definition.Properties.Single(property => property.Id == member.TargetProperty).Name)];
    }

    [Fact] void should_keep_omitted_members_absent_and_preserve_supplied_order() => _names.ShouldEqual(["second", "child"]);
    [Fact] void should_preserve_nested_omission() => Assert.IsType<SemanticCompositeValue>(_details.Properties[1].Value).Properties.ShouldBeEmpty();
}
