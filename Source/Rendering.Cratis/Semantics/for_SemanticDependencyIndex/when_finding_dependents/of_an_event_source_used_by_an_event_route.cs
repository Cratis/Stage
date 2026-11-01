// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticDependencyIndex.when_finding_dependents;

public class of_an_event_source_used_by_an_event_route : given.a_dependency_index
{
    SemanticId _source;
    SemanticId _stream;

    void Establish()
    {
        _source = Identity("source");
        _stream = Identity("stream");
        _application = _application with
        {
            EventSources = [new(_source, "Project", "domain", [new(_stream, "Details", "domain")])]
        };
        var command = _registerProject.Commands.Single();
        _registerProject = _registerProject with
        {
            Commands = [command with { Produces = [command.Produces.Single() with { Route = new(_source, _stream) }] }]
        };
    }

    void Because() => BuildIndex();

    [Fact] void should_cover_the_entire_reference_shape() => Assert.True(_index.IsSemanticGraphComplete, string.Join(Environment.NewLine, _index.IncompleteReasons));
    [Fact] void should_make_the_producing_command_depend_on_the_event_source() => _index.Dependents(_source).ShouldContain(_registerProject.Commands.Single().Id);
    [Fact] void should_make_the_producing_command_depend_on_the_event_stream() => _index.Dependents(_stream).ShouldContain(_registerProject.Commands.Single().Id);
    [Fact] void should_not_make_the_event_schema_depend_on_its_production_route() => _index.Dependents(_source).ShouldNotContain(_registerProject.Events.Single().Id);
    [Fact] void should_not_make_the_event_source_depend_on_the_event_schema() => _index.Dependents(_registerProject.Events.Single().Id).ShouldNotContain(_source);
    [Fact] void should_record_the_route_on_the_production_owner() => _index.Edges.Any(edge => edge.Kind == SemanticDependencyKind.Route && edge.Member == "SemanticCommandRoute.Source" && edge.Dependency == _source && edge.Dependent == _registerProject.Commands.Single().Id).ShouldBeTrue();
}
