// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticDependencyIndex.when_finding_dependents;

public class of_a_constraint_used_by_specification_appends : given.a_constraint_dependency
{
    static SemanticSpecification Appending(SemanticId owner, SemanticId contract) => new(owner, owner.ToString(), [], [], null, [], [], [], []) { WhenAppended = new(contract, []) };

    void Establish() => _registerProject = _registerProject with { Specifications = [Appending(_producer, _constrained.Id), Appending(_control, _unconstrained.Id)] };

    void Because() => BuildIndex();

    [Fact] void should_require_the_constraint_owner() => ShouldRequireConstraint();
    [Fact] void should_not_include_an_unrelated_append() => ShouldExcludeUnrelatedProducer();
    [Fact] void should_record_the_direct_constraint_edge() => ShouldRecordConstraintEdge();
    [Fact] void should_cover_the_semantic_references() => ShouldCoverReferences();
}
