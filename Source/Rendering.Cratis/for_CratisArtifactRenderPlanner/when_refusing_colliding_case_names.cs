// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_refusing_colliding_case_names : a_specification_case_table
{
    async Task Establish() => _loaded = await when_rendering_a_pure_reducer.Load(Source.Replace("case Medium", "case large", StringComparison.Ordinal));

    void Because() => _plan = when_rendering_a_pure_reducer.Plan(_loaded);

    [Fact] void should_refuse_the_plan() => _plan.Success.ShouldBeFalse();
    [Fact] void should_report_the_normalized_name_collision() => _plan.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldContain("STAGE-CRATIS-004");
}
#endif
