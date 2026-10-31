// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticDependencyIndex.when_finding_dependents;

public class of_a_property_referenced_by_a_collection_membership_test : given.a_dependency_index
{
    void Establish()
    {
        var command = _registerProject.Commands.Single();
        var produced = command.Produces.Single();
        var mapping = produced.Mappings[0];
        _registerProject = _registerProject with
        {
            Commands = [command with
            {
                Produces = [produced with
                {
                    Mappings = [mapping with { Source = new SemanticOneOfExpression(_projectLookup.ReadModels.Single().Properties[0].Id) }]
                }]
            }]
        };
    }

    void Because() => BuildIndex();

    [Fact] void should_classify_the_reference_members() => _index.IncompleteReasons.Any(reason => reason.Contains("unclassified reference-carrying member", StringComparison.Ordinal)).ShouldBeFalse();
    [Fact] void should_not_claim_completeness_for_the_unsupported_expression() => _index.IsSemanticGraphComplete.ShouldBeFalse();
    [Fact] void should_retain_the_unsupported_variant_reason() => _index.IncompleteReasons.Any(reason => reason.Contains("unknown variant 'SemanticOneOfExpression'", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_find_the_command_depending_on_the_collection_owner() => _index.Dependents(_projectLookup.ReadModels.Single().Id).ShouldContain(_registerProject.Commands.Single().Id);
    [Fact] void should_record_the_collection_property_edge() => _index.Edges.Any(edge => edge.Member == "SemanticOneOfExpression.CollectionProperty" && edge.Kind == SemanticDependencyKind.Expression && edge.Dependency == _projectLookup.ReadModels.Single().Id && edge.Dependent == _registerProject.Commands.Single().Id).ShouldBeTrue();
}
