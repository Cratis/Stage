// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticDependencyIndex.when_finding_dependents;

public class of_a_constraint_used_by_nested_capture_appends : given.a_constraint_dependency
{
    void Establish() => _registerProject = _registerProject with { Captures = [WithNested(_producer, _constrained.Id), WithNested(_control, _unconstrained.Id)] };

    SemanticCapture WithNested(SemanticId owner, SemanticId contract) => Capture(owner, contract) with { Appends = [], Nested = [new("details", [], [Append(contract)])] };

    void Because() => BuildIndex();

    [Fact] void should_require_the_constraint_owner() => ShouldRequireConstraint();
    [Fact] void should_not_include_an_unrelated_capture() => ShouldExcludeUnrelatedProducer();
    [Fact] void should_record_the_direct_constraint_edge() => ShouldRecordConstraintEdge();
    [Fact] void should_cover_the_semantic_references() => ShouldCoverReferences();
}
