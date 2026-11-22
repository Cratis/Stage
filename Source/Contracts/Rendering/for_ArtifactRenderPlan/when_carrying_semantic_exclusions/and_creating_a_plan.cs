// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Rendering.for_ArtifactRenderPlan.when_carrying_semantic_exclusions;

public class and_creating_a_plan : given.a_request_with_exclusions
{
    void Because() => _result = Create();

    [Fact] void should_retain_both_typed_entries() => _result.Exclusions.Length.ShouldEqual(2);
    [Fact] void should_warn_once_per_exclusion() => _result.Diagnostics.Length.ShouldEqual(2);
    [Fact] void should_own_warnings_on_the_retained_parent() => _result.Diagnostics.All(diagnostic => diagnostic.Artifact == _slice.ParentId).ShouldBeTrue();
    [Fact] void should_list_all_original_codes() => _result.Diagnostics.Single(diagnostic => diagnostic.Message.Contains(_slice.Address, StringComparison.Ordinal)).Message.Contains("PLAY0053, PLAY0070", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_emit_unsupported_plan_evidence() => _result.PlanEvidence.All(evidence => evidence.Diagnostic.Code == ArtifactRenderDegradationCodes.UnsupportedConstruct && evidence.Remedy.Contains("recompile", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_not_invent_leaf_evidence() => _result.Degradations.IsEmpty.ShouldBeTrue();
    [Fact] void should_be_degraded() => _result.Outcome.ShouldEqual(ArtifactRenderOutcome.Degraded);
    [Fact] void should_publish_the_admissible_output() => _result.CanPublishArtifacts.ShouldBeTrue();
}
