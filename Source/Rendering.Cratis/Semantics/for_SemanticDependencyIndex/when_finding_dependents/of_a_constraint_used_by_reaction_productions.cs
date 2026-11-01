// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticDependencyIndex.when_finding_dependents;

public class of_a_constraint_used_by_reaction_productions : given.a_constraint_dependency
{
    void Establish() => _registerProject = _registerProject with { Reactions = [Reaction(_producer, _constrained.Id), Reaction(_control, _unconstrained.Id)] };

    void Because() => BuildIndex();

    [Fact] void should_require_the_constraint_owner() => ShouldRequireConstraint();
    [Fact] void should_not_include_an_unrelated_reaction() => ShouldExcludeUnrelatedProducer();
    [Fact] void should_record_the_direct_constraint_edge() => ShouldRecordConstraintEdge();
    [Fact] void should_cover_the_semantic_references() => ShouldCoverReferences();
}
