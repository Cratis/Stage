// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticCratisAdmission.when_rejecting_a_from_block;

public class with_an_unknown_event : given.a_scoped_from_block
{
    void Establish()
    {
        _from = _from with { EventContract = SemanticId.Create(SemanticKind.EventContract, "missing-event") };
    }

    void Because() => Evaluate();

    [Fact] void should_identify_the_failure() => ShouldExplain("the event contract cannot be resolved");
    [Fact] void should_include_the_unresolved_event_identity() => ShouldExplain(_from.EventContract.ToString());
    [Fact] void should_keep_the_projection_semantic_identity() => _diagnostic.Artifact.IsSet.ShouldBeTrue();
}
