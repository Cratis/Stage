// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticCratisAdmission.when_rejecting_a_from_block;

public class with_an_unsupported_child_parent_key : given.a_scoped_from_block
{
    void Establish()
    {
        _child = true;
        _from = _scope.Children.Single().Scope.From.Single() with { ParentKey = new SemanticProjectionValueKey(SemanticProjectionValue.EventContext("occurred")) };
    }

    void Because() => Evaluate();

    [Fact] void should_identify_the_failure() => ShouldExplain("parent key '$context.occurred'");
    [Fact] void should_explain_why() => ShouldExplain("event-context keys have no admitted Chronicle lowering");
    [Fact] void should_keep_the_projection_semantic_identity() => _diagnostic.Artifact.IsSet.ShouldBeTrue();
}
