// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Contracts.Queries;
using Cratis.Chronicle.Contracts.Sequences;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.Schemas;
using Cratis.Specifications;
using Cratis.Stage.Host.for_SemanticChronicleRegistration.given;
using Cratis.Stage.Semantics;
using NSubstitute;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticChronicleRegistration;

public class when_restarting_over_a_legacy_event_type_with_a_seeded_decimal : a_seeding_store
{
    Exception? _error;
    string _legacy = null!;

    void Establish()
    {
        Prepare("""
            seed
              for "mug"
                ProductListed
                  name = "Stoneware mug"
                  price = 12345678901234567.89
                  featured = true
                  tags none
            """,
            tail: 0,
            exactPrice: "12345678901234567.89");
        _sourcesWithEvents.Add("mug");
        var contract = _plan.Events.Values.Single();
        _legacy = SemanticSchemas.Schema(contract.Properties, _plan.Model.Application);
        _storedSchemas[SemanticStoredEventNames.Of(contract)] = _legacy;
        var history = SemanticSeeds.Stored(_plan, _seeds.Groups.SelectMany(group => group.Facts), 0, _seeds.Occurred).ToArray();
        var schema = JsonSchema.FromJson(_legacy);
        var converter = new ExpandoObjectConverter(new TypeFormats());
        foreach (var stored in history)
        {
            stored.Content = JsonSerializer.Serialize(converter.ToExpandoObject(JsonNode.Parse(stored.Content).AsObject(), schema));
        }

        _contracts.Sequences.FromSequenceNumber(Arg.Any<FromSequenceNumberRequest>()).Returns(
            QueryResult<IEnumerable<AppendedEventResponse>>.Success(Guid.Empty, history));
    }

    async Task Because() => _error = await Catch.Exception(async () => await SemanticChronicleRegistration.Register(_client, Store, _plan, _seeds));

    [Fact] void should_keep_the_stored_shape() => _storedSchemas.Values.Single().ShouldEqual(_legacy);
    [Fact] void should_refuse_the_log_whose_seed_decimal_was_not_kept_exactly() => _error.ShouldBeOfExactType<SemanticWorldRebuildRefused>();
}
