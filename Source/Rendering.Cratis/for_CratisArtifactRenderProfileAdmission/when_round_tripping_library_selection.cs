// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Contracts.Scene;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderProfileAdmission;

public class when_round_tripping_library_selection : Specification
{
    ArtifactRenderProfile _default = null!;
    ArtifactRenderProfile _prime = null!;

    void Because()
    {
        _default = Profile([]);
        _prime = Profile(["PrimeReact"]);
    }

    [Fact] void should_admit_the_default_scaffold() => CratisArtifactRenderProfileAdmission.MatchesScaffold(_default, "App", out _, out _).ShouldBeTrue();
    [Fact] void should_admit_the_prime_scaffold() => CratisArtifactRenderProfileAdmission.MatchesScaffold(_prime, "App", out _, out _).ShouldBeTrue();
    [Fact] void should_refuse_prime_scaffold_bytes_for_a_default_scene() => CratisArtifactRenderProfileAdmission.MatchesScaffold(WithScene(_prime, _default), "App", out _, out _).ShouldBeFalse();
    [Fact] void should_refuse_default_scaffold_bytes_for_a_prime_scene() => CratisArtifactRenderProfileAdmission.MatchesScaffold(WithScene(_default, _prime), "App", out _, out _).ShouldBeFalse();

    static ArtifactRenderProfile Profile(string[] packages) => CratisRendering.CreateProfile(
        "App",
        new("App", "App"),
        new SceneApplication([new("App", "web", packages)], [], [], [], [], []));

    static ArtifactRenderProfile WithScene(ArtifactRenderProfile scaffold, ArtifactRenderProfile scene) => ArtifactRenderProfile.Create(
        scaffold.Target,
        scaffold.TargetVersion,
        scaffold.Renderer,
        scaffold.RendererVersion,
        [.. scaffold.Inputs.Where(input => input.Name != "cratis-scaffold:text:scene.json"), scene.Inputs.Single(input => input.Name == "cratis-scaffold:text:scene.json")]);
}
