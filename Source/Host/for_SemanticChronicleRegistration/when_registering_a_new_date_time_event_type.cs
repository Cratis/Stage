// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Semantics;
using Cratis.Stage.Host.for_SemanticChronicleRegistration.given;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticChronicleRegistration;

public class when_registering_a_new_date_time_event_type : a_date_time_store
{
    readonly List<SemanticModelLoadDiagnostic> _notes = [];

    void Establish() => PrepareDateTime();
    async Task Because() => await SemanticChronicleRegistration.Register(_client, Store, new SemanticReplayPlan(_plan), _seeds, notes: _notes);

    [Fact] void should_register_the_concept_as_a_plain_string() => _registered["properties"]!["at"]!["type"]!.GetValue<string>().ShouldEqual("string");
    [Fact] void should_not_register_a_lossy_format() => _registered["properties"]!["at"]!.AsObject().ContainsKey("format").ShouldBeFalse();
    [Fact] void should_not_warn_about_lossy_storage() => _notes.ShouldBeEmpty();
}
