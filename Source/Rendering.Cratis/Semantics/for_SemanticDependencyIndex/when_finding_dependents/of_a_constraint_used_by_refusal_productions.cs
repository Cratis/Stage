// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticDependencyIndex.when_finding_dependents;

public class of_a_constraint_used_by_refusal_productions : given.a_constraint_dependency
{
    void Establish()
    {
        var invoked = Identity("invoked-command");
        _registerProject = _registerProject with
        {
            Commands = [Command(invoked, _unconstrained.Id)],
            Reactions = [OnRefused(_producer, invoked, _constrained.Id), OnRefused(_control, invoked, _unconstrained.Id)]
        };
    }

    SemanticReaction OnRefused(SemanticId owner, SemanticId command, SemanticId contract) => new(owner, owner.ToString(), [new(SemanticReactionTriggerKind.Event)
    {
        Source = _unconstrained.Id,
        Invokes = [new(command, []) { OnRefused = [new("Any", null, [Produced(contract)])] }]
    }]);

    void Because() => BuildIndex();

    [Fact] void should_require_the_constraint_owner() => ShouldRequireConstraint();
    [Fact] void should_not_include_an_unrelated_refusal_branch() => ShouldExcludeUnrelatedProducer();
    [Fact] void should_record_the_direct_constraint_edge() => ShouldRecordConstraintEdge();
    [Fact] void should_cover_the_semantic_references() => ShouldCoverReferences();
}
