// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Semantics;
using Cratis.Stage.Host.for_SemanticChronicleRegistration.given;
using Cratis.Stage.Semantics;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticChronicleRegistration;

public class when_registering_a_decimal_event_type_the_kernel_does_not_hold : a_seeding_store
{
    readonly List<SemanticModelLoadDiagnostic> _notes = [];
    JsonNode _registered = null!;

    void Establish() => Prepare("seed\n  for \"mug\"\n    ProductListed\n      name = \"Mug\"\n      price = 12\n      featured = true\n      tags none");

    async Task Because()
    {
        await SemanticChronicleRegistration.Register(_client, Store, new SemanticReplayPlan(_plan), _seeds, notes: _notes);
        _registered = JsonNode.Parse(_storedSchemas.Values.Single())!;
    }

    [Fact] void should_register_the_decimal_with_its_exact_format() => _registered["properties"]!["price"]!["format"]!.GetValue<string>().ShouldEqual("decimal");
    [Fact] void should_not_warn() => _notes.ShouldBeEmpty();
}
