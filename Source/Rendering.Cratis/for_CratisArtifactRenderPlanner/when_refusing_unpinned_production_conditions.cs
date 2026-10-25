// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_refusing_unpinned_production_conditions : Specification
{
    ArtifactRenderPlan _plan = null!;

    void Because() => _plan = invoice_model.Plan(invoice_model.Compile(when_rendering_conditional_generated_productions.Source
        .Replace("enabled == true", "receipt == \"ABCDEFGHIJKLMNOPQRSTU\" and enabled == true", StringComparison.Ordinal)
        .Replace("          generated receipt = \"ABCDEFGHIJKLMNOPQRSTU\"\n", string.Empty, StringComparison.Ordinal)));

    [Fact] void should_refuse_unknown_production_selection() => _plan.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldContain("STAGE-ESM-038");
    [Fact] void should_omit_the_unpinned_production() => rendering_refusals.Rejected(_plan, "STAGE-ESM-038");
}
