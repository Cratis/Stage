// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticDependencyIndex.when_finding_dependents;

public class of_a_constraint_used_by_projection_publications : given.a_constraint_dependency
{
    static SemanticProjection Publication(SemanticId owner, SemanticId contract) => new(owner, owner.ToString(), contract, []) { Target = SemanticProjectionTargetKind.Event };

    void Establish() => _registerProject = _registerProject with { Projections = [Publication(_producer, _constrained.Id), Publication(_control, _unconstrained.Id)] };

    void Because() => BuildIndex();

    [Fact] void should_require_the_constraint_owner() => ShouldRequireConstraint();
    [Fact] void should_not_include_an_unrelated_publication() => ShouldExcludeUnrelatedProducer();
    [Fact] void should_record_the_direct_constraint_edge() => ShouldRecordConstraintEdge();
    [Fact] void should_cover_the_semantic_references() => ShouldCoverReferences();
}
