// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticDependencyIndex.when_finding_dependents;

public class of_a_constraint_used_by_reducer_publications : given.a_constraint_dependency
{
    void Establish()
    {
        var observed = _unconstrained with { Id = Identity("observed-event"), Name = "Observed" };
        _producer = _constrained.Id;
        _control = _unconstrained.Id;
        _registerProject = _registerProject with
        {
            Events = [.. _registerProject.Events, observed],
            Reducers = [Publication(_producer, observed.Id), Publication(_control, observed.Id)]
        };
    }

    static SemanticReducer Publication(SemanticId contract, SemanticId observed) => new(contract.ToString(), contract, [new(observed, "publish")]) { Target = SemanticProjectionTargetKind.Event };

    void Because() => BuildIndex();

    [Fact] void should_require_the_constraint_owner_in_another_slice() => ShouldRequireConstraint();
    [Fact] void should_not_include_an_unrelated_publication() => ShouldExcludeUnrelatedProducer();
    [Fact] void should_record_the_direct_constraint_edge() => ShouldRecordConstraintEdge();
    [Fact] void should_cover_the_semantic_references() => ShouldCoverReferences();
}
