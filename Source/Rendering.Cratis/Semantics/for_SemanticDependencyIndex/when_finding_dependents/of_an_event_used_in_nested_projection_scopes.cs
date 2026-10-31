// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticDependencyIndex.when_finding_dependents;

public class of_an_event_used_in_nested_projection_scopes : given.a_dependency_index
{
    void Establish()
    {
        var contract = _registerProject.Events.Single();
        var projection = _projectLookup.Projections.Single();
        var property = _projectLookup.ReadModels.Single().Properties[0];
        var scope = SemanticProjectionScope.Empty with
        {
            Joins = [new(contract.Id, property.Id, [])],
            Removals = [new(contract.Id, new SemanticProjectionValueKey(new SemanticProjectionEventSourceIdentity()), null)]
        };
        _projectLookup = _projectLookup with
        {
            Projections = [projection with { Transitions = [], Scope = SemanticProjectionScope.Empty with { Nested = [new(property.Id, scope)] } }]
        };
    }

    void Because() => BuildIndex();

    [Fact] void should_include_the_nested_projection_owner() => _index.Dependents(_registerProject.Events.Single().Id).ShouldContain(_projectLookup.Projections.Single().Id);
    [Fact] void should_include_its_read_model_owner() => _index.Dependents(_registerProject.Events.Single().Id).ShouldContain(_projectLookup.ReadModels.Single().Id);
    [Fact] void should_record_nested_join_references() => _index.Edges.Any(edge => edge.Member == "SemanticProjectionJoin.EventContract").ShouldBeTrue();
    [Fact] void should_record_nested_removal_references() => _index.Edges.Any(edge => edge.Member == "SemanticProjectionRemoval.EventContract").ShouldBeTrue();
}
