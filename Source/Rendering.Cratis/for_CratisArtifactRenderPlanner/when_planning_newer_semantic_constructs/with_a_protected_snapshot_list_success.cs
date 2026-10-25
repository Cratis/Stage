// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_planning_newer_semantic_constructs;

public class with_a_protected_snapshot_list_success : Specification
{
    ArtifactRenderPlan _plan = null!;

    void Because() => _plan = invoice_model.Plan(invoice_model.Compile(a_protected_collection_model.Snapshot));

    [Fact] void should_record_partial_admission() => rendering_refusals.Partial(_plan);
    [Fact] void should_report_the_collection_store_gap() => _plan.Diagnostics.Any(diagnostic => diagnostic.Code == "STAGE-ESM-011" && diagnostic.Message.Contains("GetInstances", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_omit_the_refused_parts_and_compile_the_rest() => rendering_refusals.OmittedParts(_plan);
}
