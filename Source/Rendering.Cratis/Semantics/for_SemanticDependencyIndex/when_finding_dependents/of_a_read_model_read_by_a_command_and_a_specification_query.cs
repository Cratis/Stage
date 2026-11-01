// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticDependencyIndex.when_finding_dependents;

public class of_a_read_model_read_by_a_command_and_a_specification_query : given.a_dependency_index
{
    void Establish()
    {
        var command = _registerProject.Commands.Single();
        var model = _projectLookup.ReadModels.Single();
        var produced = command.Produces.Single();
        _registerProject = _registerProject with
        {
            Commands = [command with
            {
                Reads = [new("existing", model.Id, command.Properties[0].Id, SemanticDecisionReadPresence.Required)],
                Produces = [produced with { Mappings = [produced.Mappings[0] with { Source = new SemanticReadPathExpression("existing", model.Properties[0].Id) }] }]
            }],
            Specifications = [_registerProject.Specifications[0] with
            {
                ThenQueries = [new(_projectLookup.Queries.Single().Id, new SemanticTextValue("project-1"), [])]
            }]
        };
    }

    void Because() => BuildIndex();

    [Fact] void should_cover_the_entire_reference_shape() => Assert.True(_index.IsSemanticGraphComplete, string.Join(Environment.NewLine, _index.IncompleteReasons));

    [Fact] void should_include_the_reading_command() => _index.Dependents(_projectLookup.ReadModels.Single().Id).ShouldContain(_registerProject.Commands.Single().Id);
    [Fact] void should_include_the_query_consumer_specification() => _index.Dependents(_projectLookup.ReadModels.Single().Id).ShouldContain(_registerProject.Specifications[0].Id);
    [Fact] void should_classify_the_decision_read() => _index.Edges.Any(edge => edge.Member == "SemanticDecisionRead.ReadModel" && edge.Kind == SemanticDependencyKind.ReadModelRead).ShouldBeTrue();
    [Fact] void should_resolve_a_read_path_property_to_its_model_owner() => _index.Edges.Any(edge => edge.Member == "SemanticReadPathExpression.PropertyId" && edge.Dependency == _projectLookup.ReadModels.Single().Id).ShouldBeTrue();
    [Fact] void should_classify_the_specification_query_reference() => _index.Edges.Any(edge => edge.Member == "SemanticSpecificationQueryResult.Query" && edge.Kind == SemanticDependencyKind.Specification).ShouldBeTrue();
}
