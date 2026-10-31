// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticDependencyIndex.when_finding_dependents;

public class of_a_concept_used_through_a_composite_event_property : given.a_dependency_index
{
    SemanticId _composite;
    SemanticId _unrelated;

    void Establish()
    {
        _composite = Identity("composite");
        _unrelated = Identity("unrelated");
        var contract = _registerProject.Events.Single();
        var property = contract.Properties.Single(property => property.Name == "name");
        _application = _application with
        {
            Types = [new(_composite, "CompositeName", [property with { Id = Identity("composite-member") }]), new(_unrelated, "Unrelated", [])]
        };
        _registerProject = _registerProject with
        {
            Events = [contract with { Properties = [.. contract.Properties.Select(member => member.Id == property.Id ? member with { Type = SemanticTypeReference.ForCompositeType(_composite) } : member)] }]
        };
    }

    void Because() => BuildIndex();

    [Fact] void should_include_the_composite_owner() => _index.Dependents(_projectName.Id).ShouldContain(_composite);
    [Fact] void should_include_the_event_owner_transitively() => _index.Dependents(_projectName.Id).ShouldContain(_registerProject.Events.Single().Id);
    [Fact] void should_include_the_cross_slice_query_transitively() => _index.Dependents(_projectName.Id).ShouldContain(_projectLookup.Queries.Single().Id);
    [Fact] void should_exclude_an_unrelated_owner() => _index.Dependents(_projectName.Id).ShouldNotContain(_unrelated);
    [Fact] void should_record_type_reference_edges() => _index.Edges.Any(edge => edge.Dependent == _composite && edge.Dependency == _projectName.Id && edge.Kind == SemanticDependencyKind.TypeReference).ShouldBeTrue();
}
