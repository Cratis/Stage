// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticDependencyIndex.when_finding_dependents;

public class of_an_event_returned_by_a_command_handler : for_SemanticOwnerOmission.given.an_omission_context
{
    SemanticOwnerOmission? _omission;
    void Establish()
    {
        var command = _registerProject.Commands.Single();
        _registerProject = _registerProject with
        {
            Commands = [command with
            {
                Produces = [],
                Handler = new("body", 2) { PossibleEvents = [command.Produces.Single() with { Mappings = [] }] }
            }]
        };
    }

    void Because()
    {
        BuildContext();
        _omission = SemanticOwnerOmission.Compute(_context, _index, Roots(_registerProject.Events.Single().Id));
    }

    [Fact] void should_cover_the_reference_shape() => Assert.True(_index.IsSemanticGraphComplete, string.Join(Environment.NewLine, _index.IncompleteReasons));
    [Fact] void should_find_the_command_depending_on_the_returned_event() => _index.Dependents(_registerProject.Events.Single().Id).ShouldContain(_registerProject.Commands.Single().Id);
    [Fact] void should_prove_the_owner_omission() => _omission.ShouldNotBeNull();
    [Fact] void should_include_the_handler_command_in_the_omission() => _omission!.Closure.ShouldContain(_registerProject.Commands.Single().Id);
    [Fact] void should_not_omit_the_upstream_concept() => _omission!.Closure.ShouldNotContain(_projectName.Id);
    [Fact] void should_record_the_handler_event_edge() => _index.Edges.Any(edge => edge.Member == "SemanticProducedEvent.EventContract" && edge.Kind == SemanticDependencyKind.ProducedEvent && edge.Dependency == _registerProject.Events.Single().Id && edge.Dependent == _registerProject.Commands.Single().Id).ShouldBeTrue();
}
