// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_refusing_datetime_query_filters : Specification
{
    ArtifactRenderPlan _plan = null!;

    void Because() => _plan = invoice_model.Plan(invoice_model.Compile(a_query_filter_model.Source.Replace("status String", "status DateTime", StringComparison.Ordinal)));

    [Fact] void should_refuse_lossy_text_to_instant_equality() => _plan.Diagnostics.Any(diagnostic => diagnostic.Code == "STAGE-ESM-010" && diagnostic.Message.Contains("status", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_omit_the_refused_parts_and_compile_the_rest() => rendering_refusals.OmittedParts(_plan);
}
