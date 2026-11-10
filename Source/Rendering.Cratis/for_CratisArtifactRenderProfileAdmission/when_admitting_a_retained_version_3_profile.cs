// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.Scaffolding;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderProfileAdmission;

public class when_admitting_a_retained_version_3_profile : Specification
{
    ArtifactRenderProfile _retained = null!;

    void Because() => _retained = CratisRendering.CreateProfile(
        "App",
        new("App", "App"),
        null,
        null,
        null,
        null,
        false,
        CratisBackendApplicationScaffoldProfile.Version3);

    [Fact] void should_admit_the_profile_for_version_3() => CratisArtifactRenderProfileAdmission.MatchesScaffold(_retained, "App", "3", out _, out _).ShouldBeTrue();
    [Fact] void should_refuse_the_profile_for_version_4() => CratisArtifactRenderProfileAdmission.MatchesScaffold(_retained, "App", "4", out _, out _).ShouldBeFalse();
    [Fact] void should_refuse_the_profile_for_the_default_version() => CratisArtifactRenderProfileAdmission.MatchesScaffold(_retained, "App", out _, out _).ShouldBeFalse();
    [Fact] void should_plan_the_scaffold_for_version_3() => CratisRendering.PlanScaffold(new("App", "App", "App") { ProfileVersion = "3" }).Success.ShouldBeTrue();
}
