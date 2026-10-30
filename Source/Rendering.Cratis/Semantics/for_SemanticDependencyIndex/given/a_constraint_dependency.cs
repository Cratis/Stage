// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticDependencyIndex.given;

public abstract class a_constraint_dependency : a_dependency_index
{
    protected SemanticEventContract _constrained = null!;
    protected SemanticEventContract _unconstrained = null!;
    protected SemanticId _producer;
    protected SemanticId _control;

    protected static SemanticProducedEvent Produced(SemanticId contract) => new(contract, null, null, []);

    protected static SemanticCommand Command(SemanticId owner, SemanticId contract) => new(owner, owner.ToString(), [], [], [Produced(contract)]);

    void Establish()
    {
        _constrained = _registerProject.Events.Single();
        _unconstrained = _constrained with { Id = Identity("unconstrained-event"), Name = "Unconstrained", Properties = [] };
        _producer = Identity("producer");
        _control = Identity("unrelated-producer");
        _registerProject = _registerProject with { Events = [_constrained, _unconstrained], Commands = [], Specifications = [] };
        _projectLookup = _projectLookup with
        {
            Projections = [],
            ReadModels = [],
            Queries = [],
            Specifications = [],
            Constraints = [new("UniqueProject", SemanticConstraintKind.UniqueEventOccurrence, SemanticConstraintScope.EventSequence, [new(_constrained.Id, [])], [], false, null)]
        };
    }

    protected SemanticCaptureAppend Append(SemanticId contract) => new(contract, SemanticTypeReference.ForConcept(_projectId.Id), null, []);

    protected SemanticCapture Capture(SemanticId owner, SemanticId contract) => new(owner, owner.ToString(), "id", [], [Append(contract)]);

    protected SemanticReaction Reaction(SemanticId owner, SemanticId contract) => new(owner, owner.ToString(), [new(SemanticReactionTriggerKind.Event) { Source = _unconstrained.Id, Produces = [Produced(contract)] }]);

    protected void ShouldRequireConstraint() => _index.Dependents(_projectLookup.Id).ShouldContain(_producer);

    protected void ShouldExcludeUnrelatedProducer() => _index.Dependents(_projectLookup.Id).ShouldNotContain(_control);

    protected void ShouldRecordConstraintEdge() => _index.Edges.Any(edge => edge.Kind == SemanticDependencyKind.Constraint && edge.Dependent == _producer && edge.Dependency == _projectLookup.Id).ShouldBeTrue();

    protected void ShouldCoverReferences() => Assert.True(_index.IsSemanticGraphComplete, string.Join(Environment.NewLine, _index.IncompleteReasons));
}
