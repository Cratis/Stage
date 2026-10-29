// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_rendering_opaque_policy_specifications;

public class with_uncontrolled_query_time : Specification
{
    ArtifactRenderPlan _plan = null!;

    void Because() => _plan = opaque_policy_model.Plan(opaque_policy_specification_model.Source("return context.Occurred.Year == 2027;"));

    [Fact] void should_keep_the_application() => Assert.True(_plan.Success, opaque_policy_model.Errors(_plan));
    [Fact] void should_skip_all_unclocked_scenarios() => _plan.Refusals.Length.ShouldEqual(4);
    [Fact] void should_name_the_receipt_time_read() => Assert.All(_plan.Diagnostics.Where(diagnostic => diagnostic.Code == "STAGE-ESM-011"), diagnostic => Assert.Contains("context.Occurred", diagnostic.Message, StringComparison.Ordinal));
}
#endif
