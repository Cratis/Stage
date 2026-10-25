// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Contracts.Scene;
using Cratis.Stage.Rendering.Cratis.Scene;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderProfileAdmission;

public class when_admitting_a_host_composed_profile : Specification
{
    ArtifactRenderProfile _default = null!;
    ArtifactRenderProfile _prime = null!;
    ArtifactRenderProfile _mismatched = null!;
    ArtifactRenderProfile _library = null!;
    ArtifactRenderProfile _aria = null!;

    void Because()
    {
        _default = HostProfile(false, []);
        _prime = HostProfile(true, ["PrimeReact"]);
        _mismatched = HostProfile(false, ["PrimeReact"]);
        _library = CratisRendering.CreateProfile("App", new("App", "App"), null, null, null, null, true);
        _aria = CratisRendering.CreateProfile("App", new("App", "App"), null, null, null, null, false);
    }

    [Fact] void should_admit_the_default_profile() => CratisArtifactRenderProfileAdmission.MatchesScaffold(_default, "App", out _, out _).ShouldBeTrue();
    [Fact] void should_admit_the_prime_profile() => CratisArtifactRenderProfileAdmission.MatchesScaffold(_prime, "App", out _, out _).ShouldBeTrue();
    [Fact] void should_refuse_a_prime_scene_over_the_default_scaffold() => CratisArtifactRenderProfileAdmission.MatchesScaffold(_mismatched, "App", out _, out _).ShouldBeFalse();
    [Fact] void should_emit_the_prime_scaffold_when_chosen() => Vite(_library).ShouldNotEqual(Vite(_aria));
    [Fact] void should_emit_the_prime_main_module_when_chosen() => Main(_library).ShouldContain("PrimeReactProvider");
    [Fact] void should_emit_the_default_main_module_when_not_chosen() => Main(_aria).ShouldNotContain("PrimeReactProvider");
    [Fact] void should_match_the_scene_derived_scaffold_for_prime() => Vite(_library).ShouldEqual(Vite(CratisRendering.CreateProfile("App", new("App", "App"), SceneWith(["PrimeReact"]))));
    [Fact] void should_match_the_scene_derived_scaffold_for_default() => Vite(_aria).ShouldEqual(Vite(CratisRendering.CreateProfile("App", new("App", "App"), SceneWith([]))));

    static SceneApplication SceneWith(string[] packages) => new([new("App", "web", packages)], [], [], [], [], []);

    // The CLI passes no scene and appends its own scene.json text input.
    static ArtifactRenderProfile HostProfile(bool usesPrimeReact, string[] packages)
    {
        var scaffold = CratisRendering.CreateProfile("App", new("App", "App"), null, null, null, null, usesPrimeReact);
        return ArtifactRenderProfile.Create(
            scaffold.Target,
            scaffold.TargetVersion,
            scaffold.Renderer,
            scaffold.RendererVersion,
            [.. scaffold.Inputs, CratisArtifactRenderInput.CreateText("scene.json", CratisRendering.TargetVersion, CanonicalSceneJson.Serialize(SceneWith(packages)))]);
    }

    static string Text(ArtifactRenderProfile profile, string name) => System.Text.Encoding.UTF8.GetString(profile.Inputs.Single(input => input.Name.EndsWith(name, StringComparison.Ordinal)).Bytes.AsSpan());

    static string Vite(ArtifactRenderProfile profile) => Text(profile, ".frontend/vite.config.ts");

    static string Main(ArtifactRenderProfile profile) => Text(profile, ".frontend/main.tsx");
}
