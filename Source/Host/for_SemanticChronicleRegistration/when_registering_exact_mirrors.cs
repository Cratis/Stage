// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.ReadModels;
using Cratis.Specifications;
using Cratis.Stage.Host.for_SemanticChronicleRegistration.given;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticChronicleRegistration;

public class when_registering_exact_mirrors : a_chronicle_registration
{
    async Task Because() => _world = await SemanticChronicleRegistration.Register(_client, "Projects", _plan, _mirrorIssues);

    [Fact] void should_register_events_before_read_models_and_projections() => _registrations.ToArray().ShouldEqual(["events", "models", "projections"]);
    [Fact] void should_register_the_read_model_with_the_named_store() => _models!.EventStore.ShouldEqual("Projects");
    [Fact] void should_register_the_read_model_as_client_owned() => _models!.Owner.ShouldEqual(ReadModelOwner.Client);
    [Fact] void should_register_the_read_model_for_the_workbench() => _models!.ReadModels.Single().DisplayName.ShouldEqual("ProjectSummary");
    [Fact] void should_register_the_projection_with_the_named_store() => _projections!.EventStore.ShouldEqual("Projects");
    [Fact] void should_connect_the_read_model_to_its_projection() => _models!.ReadModels.Single().ObserverIdentifier.ShouldEqual(_projections!.Projections.Single().Identifier);
    [Fact] void should_connect_the_projection_to_its_read_model() => _projections!.Projections.Single().ReadModel.ShouldEqual(_models!.ReadModels.Single().Type.Identifier);
    [Fact] void should_observe_the_event_log() => _projections!.Projections.Single().EventSequenceId.ShouldEqual("event-log");
    [Fact] void should_map_the_complete_event_transition() => _projections!.Projections.Single().From.Single().Value.Properties.Count.ShouldEqual(2);
    [Fact] void should_not_report_mirror_issues() => _mirrorIssues.ShouldBeEmpty();
    [Fact] void should_rebuild_the_independent_semantic_world() => _world.ReadModels.Single().Key.ShouldEqual(_commandValues[0].Value);
}
