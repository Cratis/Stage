// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticCratisAdmission.when_rejecting_a_from_block;

public class with_an_unsupported_subtract_source : given.a_scoped_from_block
{
    void Establish()
    {
        Map(_from.Mappings.Single(mapping => mapping.Operation == SemanticProjectionOperation.Increment) with { Operation = SemanticProjectionOperation.Subtract, Source = SemanticProjectionValue.Literal(SemanticValue.Number(2m)) });
    }

    void Because() => Evaluate();

    [Fact] void should_identify_the_failure() => ShouldExplain("mapping #1 'visits' (Subtract)");
    [Fact] void should_explain_why() => ShouldExplain("Subtract requires an event-property source or a supported numeric zero identity");
    [Fact] void should_identify_the_from_block_and_event() => ShouldExplain("From block #2 for event 'ProjectRenamed'");
    [Fact] void should_keep_the_projection_semantic_identity() => _diagnostic.Artifact.IsSet.ShouldBeTrue();
}
