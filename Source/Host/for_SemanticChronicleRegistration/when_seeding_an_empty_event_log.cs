// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Host.for_SemanticChronicleRegistration.given;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticChronicleRegistration;

public class when_seeding_an_empty_event_log : a_seeding_store
{
    SemanticWorld _world = null!;

    void Establish() => Prepare("""
        seed
          for "mug"
            ProductListed
              name = "Stoneware mug"
              price = 12.5
              featured = true
              tags = ["kitchen"]
          for "tee"
            ProductListed
              name = "Organic cotton tee"
              price = 24
              featured = false
              tags = []
        """);

    async Task Because() => _world = await SemanticChronicleRegistration.Register(_client, Store, _plan, _seeds);

    [Fact] void should_append_each_event_source_separately_in_declaration_order() => _appends.Select(append => append.Events.Single().EventSourceId).ShouldContainOnly(["mug", "tee"]);
    [Fact] void should_append_to_the_event_log_of_the_store() => _appends.TrueForAll(append => append.EventStore == Store && append.EventSequenceId == "event-log").ShouldBeTrue();
    [Fact] void should_append_the_stored_event_type() => _appends[0].Events.Single().EventType.Id.ShouldEqual("ProductListed");
    [Fact] void should_append_the_declared_values() => JsonDocument.Parse(_appends[0].Events.Single().Content).RootElement.GetProperty("name").GetString().ShouldEqual("Stoneware mug");
    [Fact] void should_append_decimal_values_exactly() => JsonDocument.Parse(_appends[0].Events.Single().Content).RootElement.GetProperty("price").GetDecimal().ShouldEqual(12.5m);
    [Fact] void should_say_who_seeded() => _appends[0].CausedBy.Subject.ShouldEqual(SemanticChronicleRegistration.SeedSubject);
    [Fact] void should_hold_the_seeded_facts_in_the_world() => _world.Facts.Length.ShouldEqual(2);
    [Fact] void should_project_the_seeded_rows() => _world.ReadModels.Length.ShouldEqual(2);
}
