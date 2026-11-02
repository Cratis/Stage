// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticDependencyIndex.when_indexing_lifecycles;

public class with_a_workflow : given.a_dependency_index
{
    void Establish() => _application = _application with { Workflows = [new("Onboarding", Identity("lifecycle-read-model"), [new("Start", SemanticWorkflowNodeKind.Trigger) { TriggerKind = SemanticWorkflowTriggerKind.Manual }], [])] };

    void Because() => BuildIndex();

    [Fact] void should_be_incomplete() => _index.IsSemanticGraphComplete.ShouldBeFalse();
    [Fact] void should_record_the_refused_member() => _index.Incompleteness.Any(gap => gap.Kind == SemanticDependencyIncompletenessKind.RefusedMember && gap.Member == "SemanticApplication.Workflows").ShouldBeTrue();
    [Fact] void should_not_record_unclassified_members() => _index.Incompleteness.Any(gap => gap.Kind == SemanticDependencyIncompletenessKind.UnclassifiedMember).ShouldBeFalse();
    [Fact] void should_not_walk_the_lifecycle() => _index.Edges.Any(edge => edge.Member.StartsWith("SemanticState", StringComparison.Ordinal) || edge.Member.StartsWith("SemanticWorkflow", StringComparison.Ordinal)).ShouldBeFalse();
}
