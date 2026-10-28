// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticDependencyIndex.when_finding_dependents;

public class of_a_read_model_with_a_reducer_migration : given.a_dependency_index
{
    SemanticReducer _reducer = null!;

    void Establish()
    {
        _reducer = new("Projects", _projectLookup.ReadModels.Single().Id, [new(_registerProject.Events.Single().Id, "reduce-or-delete-project")]);
        _projectLookup = _projectLookup with { Projections = [], Reducers = [_reducer] };
    }

    void Because() => BuildIndex();

    [Fact] void should_cover_the_entire_reference_shape() => Assert.True(_index.IsSemanticGraphComplete, string.Join(Environment.NewLine, _index.IncompleteReasons));
    [Fact] void should_own_the_reducer_under_its_read_model_not_its_slice() => SemanticDependencyOwnership.Owner(_reducer, _projectLookup.Id).ShouldEqual(_projectLookup.ReadModels.Single().Id);
    [Fact] void should_include_the_read_model_when_its_reducer_event_is_omitted() => _index.Dependents(_registerProject.Events.Single().Id).ShouldContain(_reducer.ReadModel);
    [Fact] void should_record_the_transition_on_the_read_model_owner() => _index.Edges.Any(edge => edge.Member == "SemanticReducerTransition.EventContract" && edge.Dependency == _registerProject.Events.Single().Id && edge.Dependent == _reducer.ReadModel).ShouldBeTrue();
    [Fact] void should_include_queries_when_the_reducer_read_model_is_removed() => _index.Dependents(_reducer.ReadModel).ShouldContain(_projectLookup.Queries.Single().Id);
    [Fact] void should_cover_a_transition_that_can_remove_state() => _reducer.Result.ShouldEqual(SemanticReducerResult.StateOrDelete);
}
