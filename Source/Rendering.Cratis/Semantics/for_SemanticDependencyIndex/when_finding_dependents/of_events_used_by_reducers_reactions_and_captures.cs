// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticDependencyIndex.when_finding_dependents;

public class of_events_used_by_reducers_reactions_and_captures : given.a_dependency_index
{
    SemanticId _reaction;
    SemanticId _capture;

    void Establish()
    {
        var contract = _registerProject.Events.Single();
        _reaction = Identity("reaction");
        _capture = Identity("capture");
        _projectLookup = _projectLookup with
        {
            Projections = [],
            Reducers = [new("Projects", _projectLookup.ReadModels.Single().Id, [new(contract.Id, "reduce-project")])],
            Reactions = [new(_reaction, "RegisterAgain", [new(SemanticReactionTriggerKind.Event)
            {
                Source = contract.Id,
                Invokes = [new(_registerProject.Commands.Single().Id, [])],
                Produces = [new(contract.Id, null, null, [])]
            }])],
            Captures = [new(_capture, "ImportProjects", "projectId", [], [new(contract.Id, SemanticTypeReference.ForConcept(_projectId.Id), null, [])])
            {
                EventsSource = new([contract.Id])
            }]
        };
    }

    void Because() => BuildIndex();

    [Fact] void should_cover_the_entire_reference_shape() => Assert.True(_index.IsSemanticGraphComplete, string.Join(Environment.NewLine, _index.IncompleteReasons));

    [Fact] void should_include_the_reducer_read_model_owner_without_a_projection() => _index.Dependents(_registerProject.Events.Single().Id).ShouldContain(_projectLookup.ReadModels.Single().Id);
    [Fact] void should_include_the_event_triggered_reaction() => _index.Dependents(_registerProject.Events.Single().Id).ShouldContain(_reaction);
    [Fact] void should_include_the_command_invoking_reaction() => _index.Dependents(_registerProject.Commands.Single().Id).ShouldContain(_reaction);
    [Fact] void should_include_the_capture_source_consumer() => _index.Dependents(_registerProject.Events.Single().Id).ShouldContain(_capture);
    [Fact] void should_record_a_capture_append_reference() => _index.Edges.Any(edge => edge.Dependent == _capture && edge.Member == "SemanticCaptureAppend.EventContract").ShouldBeTrue();
    [Fact] void should_record_a_reaction_produced_event_reference() => _index.Edges.Any(edge => edge.Dependent == _reaction && edge.Member == "SemanticProducedEvent.EventContract").ShouldBeTrue();
}
