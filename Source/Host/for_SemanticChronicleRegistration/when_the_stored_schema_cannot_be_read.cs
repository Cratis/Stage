// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Semantics;
using Cratis.Stage.Host.for_SemanticChronicleRegistration.given;
using Cratis.Stage.Semantics;
using NSubstitute;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticChronicleRegistration;

public class when_the_stored_schema_cannot_be_read : a_seeding_store
{
    readonly List<SemanticModelLoadDiagnostic> _notes = [];
    JsonNode _registered = null!;

    void Establish()
    {
        Prepare("seed\n  for \"mug\"\n    ProductListed\n      name = \"Mug\"\n      price = 12\n      featured = true\n      tags none");
        _contracts.EventTypes.AllEventTypeGenerations(Arg.Any<Cratis.Chronicle.Contracts.EventTypes.AllEventTypeGenerationsRequest>()).Returns<Task<Cratis.Chronicle.Contracts.Queries.QueryResult<IEnumerable<Cratis.Chronicle.Contracts.EventTypes.EventTypeDetailsResponse>>>>(_ => throw new InvalidOperationException("unavailable"));
    }

    async Task Because()
    {
        await SemanticChronicleRegistration.Register(_client, Store, new SemanticReplayPlan(_plan), _seeds, notes: _notes);
        _registered = JsonNode.Parse(_storedSchemas.Values.Single())!;
    }

    [Fact] void should_register_the_default_shape_rather_than_guess() => _registered["properties"]!["price"]!.AsObject().ContainsKey("format").ShouldBeFalse();
    [Fact] void should_warn_that_a_restart_will_refuse_the_log() => _notes.Single().Code.ShouldEqual(SemanticStoredSchemas.LegacyDecimal);
}
