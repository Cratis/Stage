// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticSpecificationEventSources.when_checking_requires_source;

public class with_a_nested_scope_without_parent_key : a_source_observability_check
{
    [Fact]
    void should_not_observe_a_parent_source_at_the_root()
    {
        var model = composite_source_models.Nested(false);
        var slice = model.Application.Modules.Single().Features.Single().Slices.Single();
        Check(model, composite_source_models.Event(model, "DetailsChanged"), slice.Id).ShouldBeFalse();
    }

    [Fact]
    void should_be_refused_inside_a_child_at_the_semantic_boundary()
    {
        var exception = Assert.Throws<InvalidSemanticContract>(() => composite_source_models.Nested(true));
        Assert.Contains("requires a parent key", exception.Message, StringComparison.Ordinal);
    }
}
