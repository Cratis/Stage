// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticSpecificationEventSources.when_checking_requires_source;

public class with_a_child_without_parent_key : a_source_observability_check
{
    [Fact]
    void should_be_refused_at_the_semantic_boundary()
    {
        var exception = Assert.Throws<InvalidSemanticContract>(() => composite_source_models.Child(false));
        Assert.Contains("requires a parent key", exception.Message, StringComparison.Ordinal);
    }
}
