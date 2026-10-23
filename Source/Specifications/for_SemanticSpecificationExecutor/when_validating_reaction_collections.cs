// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Specifications.Commands;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor;

public class when_validating_reaction_collections : Specification
{
    SemanticExecutionPlan _plan;
    SemanticTypeReference _collection;

    void Establish()
    {
        _plan = when_running_property_keyed_removal_only_projections.Compile("""
            type Item
              labels String[]
              note String optional
            module Work
              feature Processing
                slice StateChange Record
                  command RecordItems
                    id String identifier
                    items Item[]
                    produces ItemsRecorded
                      for id
                      items = items
                  event ItemsRecorded
                    items Item[]
            """);
        _collection = _plan.Events.Values.Single().Properties.Single().Type;
    }

    [Fact]
    public void should_accept_nested_and_empty_collections_with_missing_optional_members()
    {
        var item = _plan.Model.Application.Types.Single();
        var labels = item.Properties.Single(property => property.Name == "labels");
        var value = SemanticValue.Array([
            SemanticValue.Composite([new(labels.Id, SemanticValue.Array([SemanticValue.Text("first"), SemanticValue.Text("second")]))]),
            SemanticValue.Composite([new(labels.Id, SemanticValue.Array([]))])
        ]);
        SemanticRunReactionExpressions.Validate(_plan, value, _collection, "items");
        SemanticRunReactionExpressions.Validate(_plan, SemanticValue.Array([]), _collection, "items");
        SemanticRunReactionExpressions.Validate(_plan, SemanticValue.Null, _collection with { IsOptional = true }, "items");
    }

    [Fact]
    public void should_reject_a_scalar_for_a_collection() =>
        Assert.Throws<InvalidSemanticContract>(() => SemanticRunReactionExpressions.Validate(_plan, SemanticValue.Text("wrong"), _collection, "items"));

    [Fact]
    public void should_reject_null_collection_elements_even_when_the_collection_is_optional() =>
        Assert.Throws<InvalidSemanticContract>(() => SemanticRunReactionExpressions.Validate(_plan, SemanticValue.Array([SemanticValue.Null]), _collection with { IsOptional = true }, "items"));

    [Fact]
    public void should_validate_each_nested_collection_member()
    {
        var labels = _plan.Model.Application.Types.Single().Properties.Single(property => property.Name == "labels");
        var value = SemanticValue.Array([SemanticValue.Composite([new(labels.Id, SemanticValue.Array([SemanticValue.Number(12)]))])]);
        Assert.Throws<InvalidSemanticContract>(() => SemanticRunReactionExpressions.Validate(_plan, value, _collection, "items"));
    }
}
