// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Semantics;
using Cratis.Stage.Host.for_SemanticChronicleRegistration.given;
using Cratis.Stage.Semantics;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticChronicleRegistration;

public class when_registering_a_legacy_date_time_event_type : a_date_time_store
{
    readonly List<SemanticModelLoadDiagnostic> _notes = [];

    void Establish()
    {
        PrepareDateTime();
        var contract = _plan.Events.Values.Single();
        _storedSchemas[SemanticStoredEventNames.Of(contract)] = SemanticSchemas.Schema(contract.Properties, _plan.Model.Application);
    }

    async Task Because() => await SemanticChronicleRegistration.Register(_client, Store, new SemanticReplayPlan(_plan), _seeds, notes: _notes);

    [Fact] void should_send_the_legacy_format_on_registration() => _registered["properties"]!["at"]!["format"]!.GetValue<string>().ShouldEqual("date-time");
    [Fact] void should_warn_about_the_event_type() => _notes.Single().Message.ShouldContain("TickRecorded");
    [Fact] void should_name_the_lossy_property() => _notes.Single().Message.ShouldContain("'at'");
    [Fact] void should_require_a_new_history() => _notes.Single().Message.ShouldContain("Start a new history");
}
