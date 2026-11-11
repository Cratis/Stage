// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Cratis.Stage.Rendering.Cratis.Scaffolding;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_planning_with_a_profile_for_a_scaffold_profile_version : a_register_project_render_request
{
    ArtifactRenderProfile _default = null!;
    ArtifactRenderProfile _version3 = null!;
    ArtifactRenderProfile _version4 = null!;
    ArtifactRenderPlan _version4Plan = null!;
    CratisPlanResult _version4Scaffold = null!;
    Exception _unknown = null!;

    void Because()
    {
        _default = CratisRendering.CreateProfile(_model.Application.Name, _options);
        _version3 = Create("3");
        _version4 = Create("4");
        _version4Plan = _planner.Plan(_request with { Profile = _version4 });
        _version4Scaffold = CratisRendering.PlanScaffold(new(_model.Application.Name, _options.ProjectName, _options.RootNamespace) { Profile = _version4, ProfileVersion = "4" });
        _unknown = Catch.Exception(() => Create("99"));
    }

    [Fact] void should_keep_version_3_as_the_default() => ProjectVersion(_default).ShouldEqual("3");
    [Fact] void should_create_the_default_profile_for_version_3() => Digest(_version3).ShouldEqual(Digest(_default));
    [Fact] void should_create_the_default_profile_without_a_version() => Digest(Create(null)).ShouldEqual(Digest(_default));
    [Fact] void should_record_version_4() => ProjectVersion(_version4).ShouldEqual("4");
    [Fact] void should_admit_the_version_4_profile() => _version4Plan.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldNotContain("STAGE-CRATIS-001");
    [Fact] void should_plan_artifacts_for_the_version_4_profile() => _version4Plan.Artifacts.ShouldNotBeEmpty();
    [Fact] void should_render_slice_code_for_the_version_4_profile() => _version4Plan.Artifacts.Any(artifact => artifact.RelativePath.EndsWith(".cs", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_accept_the_version_4_profile_for_a_version_4_scaffold() => _version4Scaffold.Success.ShouldBeTrue();
    [Fact] void should_target_net11_for_the_version_4_profile() => Text(_version4Scaffold, "Directory.Build.props").ShouldContain("net11.0");
    [Fact] void should_refuse_an_unknown_version() => _unknown.ShouldBeOfExactType<InvalidCratisBackendApplicationScaffold>();
    [Fact] void should_name_the_unknown_version() => _unknown.Message.ShouldContain("'99'");
    [Fact] void should_name_the_supported_versions() => _unknown.Message.ShouldContain("Supported versions are '3', '4'.");

    ArtifactRenderProfile Create(string? version) =>
        CratisRendering.CreateProfile(_model.Application.Name, _options, null, null, null, null, CratisRendering.ListsPrimeReact(null), version);

    static string ProjectVersion(ArtifactRenderProfile profile) =>
        profile.Inputs.Single(input => input.Name.StartsWith(CratisArtifactRenderProfileAdmission.ProjectInputPrefix, StringComparison.Ordinal) && input.Name.EndsWith(".csproj", StringComparison.Ordinal)).Version;

    static string Digest(ArtifactRenderProfile profile) =>
        string.Join('\n', [profile.Target, profile.TargetVersion, profile.Renderer, profile.RendererVersion, .. profile.Inputs.Select(input => $"{input.Name}|{input.Version}|{input.Sha256}")]);

    static string Text(CratisPlanResult result, string path) => Encoding.UTF8.GetString(result.Artifacts.Single(artifact => artifact.RelativePath == path).Bytes.AsSpan());
}
