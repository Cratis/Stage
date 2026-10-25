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

    [Fact]
    void should_omit_the_colliding_cases()
    {
        rendering_refusals.Partial(_plan);
        var collisions = _plan.Diagnostics.Where(diagnostic => diagnostic.Code == "STAGE-CRATIS-004" &&
            diagnostic.Message.Contains("when_registering_large.cs", StringComparison.Ordinal)).ToArray();
        Assert.NotEmpty(collisions);
        Assert.All(collisions, collision => rendering_refusals.Warning(_plan, collision.Code, collision.Artifact, collision.Message));
        Assert.DoesNotContain(_plan.Artifacts, artifact => Path.GetFileName(artifact.RelativePath) == "when_registering_large.cs");
        rendering_refusals.Compiles(_plan);
    }

    [Fact] void should_retain_the_independent_case() => Assert.Contains(_plan.Artifacts, artifact => Path.GetFileName(artifact.RelativePath) == "when_registering_small.cs");
    [Fact] void should_retain_the_independent_command() => rendering_refusals.Retained(_plan, _loaded.Model.Application.Modules.Single().Features.Single().Slices.Single().Commands.Single().Id);
    [Fact] void should_report_the_normalized_name_collision() => _plan.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldContain("STAGE-CRATIS-004");
}
#endif
