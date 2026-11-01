// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_planning_newer_semantic_constructs;

public class with_a_command_backed_protected_snapshot_list : Specification
{
    ArtifactRenderPlan _plan = null!;

    void Because() => _plan = invoice_model.Plan(invoice_model.Compile(a_protected_collection_model.CommandSnapshot));

    [Fact] void should_render_the_application() => _plan.Success.ShouldBeTrue();
    [Fact] void should_report_the_collection_store_gap() => _plan.Diagnostics.Any(diagnostic => diagnostic.Code == "STAGE-ESM-011" && diagnostic.Message.Contains("GetInstances", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_omit_the_unsupported_specification() => _plan.Artifacts.SelectMany(artifact => artifact.Sources).ShouldNotContain(_plan.Refusals.Single().Artifact);
}
