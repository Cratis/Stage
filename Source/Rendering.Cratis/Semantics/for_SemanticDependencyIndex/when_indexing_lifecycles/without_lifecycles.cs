// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticDependencyIndex.when_indexing_lifecycles;

public class without_lifecycles : given.a_dependency_index
{
    void Because() => BuildIndex();

    [Fact] void should_be_complete() => _index.IsSemanticGraphComplete.ShouldBeTrue();
    [Fact] void should_not_record_lifecycle_gaps() => _index.Incompleteness.Any(gap => gap.Member == "SemanticApplication.StateMachines" || gap.Member == "SemanticApplication.Workflows").ShouldBeFalse();
}
