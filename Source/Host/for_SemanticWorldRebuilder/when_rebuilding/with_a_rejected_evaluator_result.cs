// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticWorldRebuilder.when_rebuilding;

public class with_a_rejected_evaluator_result : Specification
{
    [Fact] void should_refuse_a_rejected_world() =>
        Assert.IsType<SemanticWorldRebuildRefused>(Catch.Exception(() => SemanticWorldRebuilder.Outcome(new SemanticRejected(
            SemanticWorld.Empty, SemanticRejectionCategory.Contract, null, "invalid occurrence"))))
            .Message.ShouldContain("invalid occurrence");

    [Fact] void should_refuse_an_unexpected_outcome() =>
        Assert.IsType<SemanticWorldRebuildRefused>(Catch.Exception(() => SemanticWorldRebuilder.Outcome(new SemanticConflict(
            SemanticWorld.Empty, SemanticConflictCategory.DecisionStateChanged, "conflict"))))
            .Message.ShouldContain("unknown outcome");
}
