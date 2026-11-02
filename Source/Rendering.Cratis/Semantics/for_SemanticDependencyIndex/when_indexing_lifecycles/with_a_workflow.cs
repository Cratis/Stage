// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticDependencyIndex.when_indexing_lifecycles;

public class with_a_workflow : given.a_dependency_index
{
    void Establish() => _application = _application with { Workflows = [new("Onboarding", _projectLookup.ReadModels.Single().Id, [new("Start", SemanticWorkflowNodeKind.Trigger) { TriggerKind = SemanticWorkflowTriggerKind.Manual }], [])] };

    void Because() => BuildIndex();

    [Fact] void should_be_complete() => _index.IsSemanticGraphComplete.ShouldBeTrue();
    [Fact] void should_not_refuse_the_lifecycle() => _index.Incompleteness.Any(gap => gap.Kind == SemanticDependencyIncompletenessKind.RefusedMember).ShouldBeFalse();
    [Fact] void should_not_record_unclassified_members() => _index.Incompleteness.Any(gap => gap.Kind == SemanticDependencyIncompletenessKind.UnclassifiedMember).ShouldBeFalse();
}
