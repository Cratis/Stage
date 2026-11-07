// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle;
using Cratis.Showrunner.Notes.Contracts.Responses;
using Cratis.Showrunner.Notes.Listing;
using Cratis.Showrunner.Notes.Publishing;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Host.Published.for_NotesChronicleArtifacts;

public class when_discovering_the_notes_artifacts : Specification
{
    IClientArtifactsProvider _artifacts = null!;

    void Because() => _artifacts = NotesChronicleArtifacts.CreateProvider();

    [Fact] void should_find_the_private_event_types() => _artifacts.EventTypes.ShouldContain(type => type.Namespace == "Cratis.Showrunner.Notes.Receiving" || type.Namespace!.StartsWith("Cratis.Showrunner.Notes.Giving"));
    [Fact] void should_find_the_public_contract_event_types() => _artifacts.EventTypes.ShouldContain(typeof(NoteAcknowledged));
    [Fact] void should_find_the_outbox_publisher() => _artifacts.Reactors.ShouldContain(typeof(OutboxPublisher));
    [Fact] void should_find_the_notes_read_model_reducer() => _artifacts.Reducers.ShouldContain(typeof(MyNote));
    [Fact] void should_find_nothing_outside_the_notes_assemblies() => _artifacts.EventTypes.Concat(_artifacts.Reactors).Concat(_artifacts.Reducers).All(type => NotesChronicleArtifacts.Assemblies.Contains(type.Assembly)).ShouldBeTrue();
    [Fact] void should_find_nothing_that_needs_a_full_registration() => NotesChronicleArtifacts.Unsupported(_artifacts).ShouldBeEmpty();
}
