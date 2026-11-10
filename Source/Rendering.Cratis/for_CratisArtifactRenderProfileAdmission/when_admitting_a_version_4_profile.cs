// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.Scaffolding;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderProfileAdmission;

public class when_admitting_a_version_4_profile : Specification
{
    ArtifactRenderProfile _current = null!;

    void Because() => _current = CratisRendering.CreateProfile("App", new("App", "App"), null, null, null, null, false, CratisBackendApplicationScaffoldProfile.Current);

    [Fact] void should_admit_the_profile_for_version_4() => CratisArtifactRenderProfileAdmission.MatchesScaffold(_current, "App", "4", out _, out _).ShouldBeTrue();
    [Fact] void should_admit_the_profile_downstream_without_a_version() => CratisArtifactRenderProfileAdmission.MatchesScaffold(_current, "App", out _, out _).ShouldBeTrue();
    [Fact] void should_refuse_the_profile_at_the_facade_without_a_version() => CratisArtifactRenderProfileAdmission.MatchesScaffold(_current, "App", null, out _, out _).ShouldBeFalse();
    [Fact] void should_refuse_the_profile_for_version_3() => CratisArtifactRenderProfileAdmission.MatchesScaffold(_current, "App", "3", out _, out _).ShouldBeFalse();
}
