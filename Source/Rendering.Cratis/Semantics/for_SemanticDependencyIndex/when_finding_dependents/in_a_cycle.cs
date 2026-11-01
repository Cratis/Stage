// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticDependencyIndex.when_finding_dependents;

public class in_a_cycle : given.a_dependency_index
{
    SemanticId _first;
    SemanticId _second;

    void Establish()
    {
        _first = Identity("cycle-first");
        _second = Identity("cycle-second");
        var property = _registerProject.Events.Single().Properties[0];
        _application = _application with
        {
            Types =
            [
                new(_first, "First", [property with { Id = Identity("first-property"), Type = SemanticTypeReference.ForCompositeType(_second) }]),
                new(_second, "Second", [property with { Id = Identity("second-property"), Type = SemanticTypeReference.ForCompositeType(_first) }])
            ]
        };
    }

    void Because() => BuildIndex();

    [Fact] void should_terminate_and_return_the_other_owner_only() => _index.Dependents(_first).SequenceEqual([_second]).ShouldBeTrue();
    [Fact] void should_not_return_a_queried_owner_through_a_cycle() => _index.Dependents(_second).SequenceEqual([_first]).ShouldBeTrue();
    [Fact] void should_not_duplicate_reachable_owners() => _index.Dependents(_first).Distinct().Count().ShouldEqual(_index.Dependents(_first).Count);
    [Fact] void should_order_results_by_semantic_identity() => _index.Dependents(_projectName.Id).SequenceEqual(_index.Dependents(_projectName.Id).OrderBy(id => id.ToString(), StringComparer.Ordinal)).ShouldBeTrue();
    [Fact] void should_return_an_empty_result_for_an_unreferenced_owner() => _index.Dependents(Identity("absent")).ShouldBeEmpty();
}
