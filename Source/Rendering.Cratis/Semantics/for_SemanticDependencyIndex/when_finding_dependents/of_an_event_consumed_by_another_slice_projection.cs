// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticDependencyIndex.when_finding_dependents;

public class of_an_event_consumed_by_another_slice_projection : given.a_dependency_index
{
    void Because() => BuildIndex();

    [Fact] void should_include_the_projection_in_project_lookup() => _index.Dependents(_registerProject.Events.Single().Id).ShouldContain(_projectLookup.Projections.Single().Id);
    [Fact] void should_include_the_project_lookup_read_model() => _index.Dependents(_registerProject.Events.Single().Id).ShouldContain(_projectLookup.ReadModels.Single().Id);
    [Fact] void should_include_the_query_transitively() => _index.Dependents(_registerProject.Events.Single().Id).ShouldContain(_projectLookup.Queries.Single().Id);
    [Fact] void should_include_the_producing_command() => _index.Dependents(_registerProject.Events.Single().Id).ShouldContain(_registerProject.Commands.Single().Id);
    [Fact] void should_exclude_the_queried_owner() => _index.Dependents(_registerProject.Events.Single().Id).ShouldNotContain(_registerProject.Events.Single().Id);
    [Fact] void should_be_complete_for_the_canonical_model() => Assert.True(_index.IsSemanticGraphComplete, string.Join(Environment.NewLine, _index.IncompleteReasons));
}
