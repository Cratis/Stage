// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_planning_newer_semantic_constructs;

public class with_a_protected_scoped_live_list : Specification
{
    ArtifactRenderPlan _plan = null!;

    void Because() => _plan = invoice_model.Plan(invoice_model.Compile(a_protected_collection_model.Scoped));

    [Fact] void should_refuse_the_scoped_live_collection() => _plan.Success.ShouldBeFalse();
    [Fact] void should_report_the_children_observation_gap() => _plan.Diagnostics.Any(diagnostic => diagnostic.Code == "STAGE-ESM-011" && diagnostic.Message.Contains("Chronicle#4816", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_emit_no_artifacts() => _plan.Artifacts.ShouldBeEmpty();
}
