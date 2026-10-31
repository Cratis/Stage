// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticDependencyIndex.when_finding_dependents;

public class of_routes_policies_and_constraints : given.a_dependency_index
{
    SemanticId _source;
    SemanticId _stream;

    void Establish()
    {
        _source = Identity("source");
        _stream = Identity("stream");
        _application = _application with
        {
            Policies = [new("Operators", new SemanticAuthenticatedCondition())],
            EventSources = [new(_source, "Project", "domain", [new(_stream, "Details", "domain")]) { IdentifierType = SemanticTypeReference.ForConcept(_projectId.Id) }]
        };
        var contract = _registerProject.Events.Single();
        _registerProject = _registerProject with
        {
            Commands = [_registerProject.Commands.Single() with { Route = new(_source, _stream), Authorization = new SemanticPolicyReference("Operators") }]
        };
        _projectLookup = _projectLookup with
        {
            Constraints = [new("UniqueProject", SemanticConstraintKind.UniquePropertyValue, SemanticConstraintScope.EventSequence, [new(contract.Id, [contract.Properties[0].Id])], [contract.Id], false, null)]
        };
    }

    void Because() => BuildIndex();

    [Fact] void should_cover_the_entire_reference_shape() => Assert.True(_index.IsSemanticGraphComplete, string.Join(Environment.NewLine, _index.IncompleteReasons));

    [Fact] void should_include_the_routed_command_for_a_source() => _index.Dependents(_source).ShouldContain(_registerProject.Commands.Single().Id);
    [Fact] void should_include_the_routed_command_for_a_stream() => _index.Dependents(_stream).ShouldContain(_registerProject.Commands.Single().Id);
    [Fact] void should_assign_a_policy_to_the_application_owner() => _index.Edges.Any(edge => edge.Kind == SemanticDependencyKind.Policy && edge.Dependency == _application.Id && edge.Dependent == _registerProject.Commands.Single().Id).ShouldBeTrue();
    [Fact] void should_assign_an_idless_constraint_to_its_slice() => _index.Edges.Any(edge => edge.Kind == SemanticDependencyKind.Constraint && edge.Dependent == _projectLookup.Id && edge.Dependency == _registerProject.Events.Single().Id).ShouldBeTrue();
    [Fact] void should_connect_a_constraint_consumer_to_the_constraint_owner() => _index.Edges.Any(edge => edge.Kind == SemanticDependencyKind.Constraint && edge.Dependent == _registerProject.Commands.Single().Id && edge.Dependency == _projectLookup.Id).ShouldBeTrue();
}
