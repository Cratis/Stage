// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Contracts.Queries;
using Cratis.Chronicle.Contracts.Sequences;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.Schemas;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Host.for_SemanticChronicleRegistration.given;
using NSubstitute;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticChronicleRegistration;

public class when_restarting_with_previously_seeded_events : a_seeding_store
{
    SemanticWorld _world = null!;
    AppendedEventResponse[] _history = [];

    void Establish()
    {
        Prepare("""
            seed
              for "mug"
                ProductListed
                  name = "Stoneware mug"
                  price = 12345678901234567.89
                  featured = true
                  tags = ["kitchen"]
              for "tee"
                ProductListed
                  name = "Organic cotton tee"
                  price = 24
                  featured = false
                  tags = []
            """, tail: 1);
        _sourcesWithEvents.UnionWith(["mug", "tee"]);
        _history = SemanticSeeds.Stored(_plan, _seeds.Groups.SelectMany(group => group.Facts), 0, _seeds.Occurred).ToArray();
        var contract = _plan.Events.Values.Single();
        var schema = JsonSchema.FromJson(SemanticSchemas.Schema(contract.Properties, _plan.Model.Application));
        var converter = new ExpandoObjectConverter(new TypeFormats());
        foreach (var stored in _history)
        {
            stored.Content = JsonSerializer.Serialize(converter.ToExpandoObject(JsonNode.Parse(stored.Content)!.AsObject(), schema));
        }
        _contracts.Sequences.FromSequenceNumber(Arg.Any<FromSequenceNumberRequest>()).Returns(
            QueryResult<IEnumerable<AppendedEventResponse>>.Success(Guid.Empty, _history));
    }

    async Task Because() => _world = await SemanticChronicleRegistration.Register(_client, Store, _plan, _seeds);

    [Fact] void should_rebuild_the_seeded_facts() => _world.Facts.Length.ShouldEqual(2);
    [Fact] void should_rebuild_the_seeded_rows() => _world.ReadModels.Length.ShouldEqual(2);
    [Fact] void should_not_append_the_seeds_again() => _appends.ShouldBeEmpty();
    [Fact] void should_have_seed_causation_on_the_stored_event_context() => _history[0].Context.CausedBy.Subject.ShouldEqual(SemanticChronicleRegistration.SeedSubject);
    [Fact] void should_exercise_a_decimal_that_storage_did_not_preserve_exactly() => JsonDocument.Parse(_history[0].Content).RootElement.GetProperty("price").GetDecimal().ShouldNotEqual(12345678901234567.89m);
}
