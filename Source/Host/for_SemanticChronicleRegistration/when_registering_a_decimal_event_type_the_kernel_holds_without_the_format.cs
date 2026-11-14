// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Semantics;
using Cratis.Stage.Host.for_SemanticChronicleRegistration.given;
using Cratis.Stage.Semantics;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticChronicleRegistration;

public class when_registering_a_decimal_event_type_the_kernel_holds_without_the_format : a_seeding_store
{
    readonly List<SemanticModelLoadDiagnostic> _notes = [];
    string _legacy = null!;

    void Establish()
    {
        Prepare("seed\n  for \"mug\"\n    ProductListed\n      name = \"Mug\"\n      price = 12\n      featured = true\n      tags none");
        var contract = _plan.Events.Values.Single();
        _legacy = SemanticSchemas.Schema(contract.Properties, _plan.Model.Application);
        _storedSchemas[SemanticStoredEventNames.Of(contract)] = _legacy;
    }

    async Task Because() => await SemanticChronicleRegistration.Register(_client, Store, new SemanticReplayPlan(_plan), _seeds, notes: _notes);

    [Fact] void should_keep_the_stored_shape() => _storedSchemas.Values.Single().ShouldEqual(_legacy);
    [Fact] void should_warn_that_a_restart_will_refuse_the_log() => _notes.Single().Code.ShouldEqual(SemanticStoredSchemas.LegacyDecimal);
}
