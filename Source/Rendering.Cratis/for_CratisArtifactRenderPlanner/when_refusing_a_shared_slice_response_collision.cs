// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_refusing_a_shared_slice_response_collision : Specification
{
    ArtifactRenderPlan _plan = null!;

    void Because() => _plan = invoice_model.Plan(invoice_model.Compile(when_rendering_several_commands.Source.Replace("ReviseInvoice", "IssueInvoiceResponse", StringComparison.Ordinal)));

    [Fact] void should_refuse_the_collision() => _plan.Success.ShouldBeFalse();
    [Fact] void should_name_the_response_capability() => _plan.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldContain("STAGE-ESM-038");
    [Fact] void should_not_emit_partial_artifacts() => _plan.Artifacts.ShouldBeEmpty();
}
