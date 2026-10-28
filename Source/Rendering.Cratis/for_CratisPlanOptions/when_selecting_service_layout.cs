// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Contracts.Semantics;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisPlanOptions;

public class when_selecting_service_layout : a_register_project_render_request
{
    const string Folder = "Source/Projects/";

    CratisPlanOptions _plan = null!;
    CratisPlanResult _default = null!;
    CratisPlanResult _explicitlyOff = null!;
    CratisPlanResult _scaffold = null!;
    CratisPlanResult _both = null!;
    CratisPlanResult _scoped = null!;
    ArtifactRenderPlan _defaultApplication = null!;
    ArtifactRenderPlan _explicitlyOffApplication = null!;
    ArtifactRenderPlan _application = null!;
    string _bothMessage = null!;

    void Establish() => _plan = new(_model.Application.Name, "Projects", "Projects");

    void Because()
    {
        var loaded = new LoadedSemanticModel(_model, _executionPlan);
        var layout = _plan with { ServiceLayout = true };
        _default = CratisRendering.PlanScaffold(_plan);
        _explicitlyOff = CratisRendering.PlanScaffold(_plan with { ServiceLayout = false });
        _scaffold = CratisRendering.PlanScaffold(layout);
        _both = CratisRendering.PlanScaffold(layout with { RepositoryRoot = true });
        _bothMessage = string.Join("\n", _both.Diagnostics.Select(diagnostic => diagnostic.Message));
        var scope = new ArtifactRenderScope(ArtifactRenderScopeKind.Application, _model.Application.Id);
        _defaultApplication = CratisRendering.Plan(_model, _executionPlan, scope, _options);
        _explicitlyOffApplication = CratisRendering.Plan(_model, _executionPlan, scope, _options with { ServiceLayout = false });
        _application = CratisRendering.Plan(_model, _executionPlan, scope, _options with { ServiceLayout = true });
        _scoped = CratisRendering.PlanFrom(loaded, new([PlanSelectionEntry.Slice(_module.Name, _feature.Name, _registerProject.Name)]), layout);
    }

    [Fact] void should_default_to_the_application_root_layout() => _plan.ServiceLayout.ShouldBeFalse();
    [Fact] void should_not_put_the_layout_in_the_default_profile() => CratisRendering.CreateProfile(_model.Application.Name, _options).Inputs.Any(input => input.Name == ServiceLayoutInput.Name).ShouldBeFalse();
    [Fact] void should_put_the_layout_in_the_profile_when_selected() => CratisRendering.CreateProfile(_model.Application.Name, _options with { ServiceLayout = true }).Inputs.Any(input => input.Name == ServiceLayoutInput.Name).ShouldBeTrue();

    // The default output stays byte-identical: switching the option off is the same as never mentioning it, and no default path is prefixed.
    [Fact] void should_keep_the_default_scaffold_plan_byte_identical() => _default.Digest.ShouldEqual(_explicitlyOff.Digest);
    [Fact] void should_keep_the_default_application_plan_byte_identical() => _defaultApplication.Digest.ShouldEqual(_explicitlyOffApplication.Digest);
    [Fact] void should_not_prefix_any_default_path() => _defaultApplication.Artifacts.Any(artifact => artifact.RelativePath.StartsWith("Source/", StringComparison.Ordinal)).ShouldBeFalse();
    [Fact] void should_keep_the_repository_files_in_the_default_scaffold() => _default.Artifacts.Select(artifact => artifact.RelativePath).ShouldContain("Directory.Packages.props");

    [Fact] void should_succeed_for_the_scaffold() => _scaffold.Success.ShouldBeTrue();
    [Fact] void should_place_every_scaffold_path_in_the_service_folder() => _scaffold.Artifacts.All(artifact => artifact.RelativePath.StartsWith(Folder, StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_plan_the_project_in_the_service_folder() => _scaffold.Artifacts.Select(artifact => artifact.RelativePath).ShouldContain($"{Folder}Projects.csproj");
    [Fact] void should_plan_the_program_in_the_service_folder() => _scaffold.Artifacts.Select(artifact => artifact.RelativePath).ShouldContain($"{Folder}Program.cs");
    [Fact] void should_plan_the_global_usings_in_the_service_folder() => _scaffold.Artifacts.Select(artifact => artifact.RelativePath).ShouldContain($"{Folder}GlobalUsings.cs");
    [Fact] void should_plan_the_frontend_package_in_the_service_folder() => _scaffold.Artifacts.Select(artifact => artifact.RelativePath).ShouldContain($"{Folder}package.json");

    [Theory]
    [InlineData(".editorconfig")]
    [InlineData("Directory.Build.props")]
    [InlineData("Directory.Build.targets")]
    [InlineData("Directory.Packages.props")]
    [InlineData("Projects.slnx")]
    [InlineData("docker-compose.yml")]
    public void should_leave_the_repository_file_to_the_repository_plan(string file) => _scaffold.Artifacts.Select(artifact => artifact.RelativePath).ShouldNotContain($"{Folder}{file}");

    [Fact] void should_keep_every_remaining_scaffold_file_byte_for_byte() => Describe(_scaffold.Artifacts, Folder).ShouldEqual(Describe(_default.Artifacts.Where(artifact => !IsRepositoryFile(artifact.RelativePath)), string.Empty));
    [Fact] void should_keep_every_remaining_application_file_byte_for_byte() => Describe(_application.Artifacts, Folder).ShouldEqual(Describe(_defaultApplication.Artifacts.Where(artifact => !IsRepositoryFile(artifact.RelativePath)), string.Empty));
    [Fact] void should_place_every_application_path_in_the_service_folder() => _application.Artifacts.All(artifact => artifact.RelativePath.StartsWith(Folder, StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_place_the_scoped_plan_in_the_service_folder() => (_scoped.Success && _scoped.Artifacts.Length > 0 && _scoped.Artifacts.All(artifact => artifact.RelativePath.StartsWith(Folder, StringComparison.Ordinal))).ShouldBeTrue();

    [Fact] void should_refuse_the_repository_root_combination() => _both.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldContain("STAGE-PLAN-028");
    [Fact] void should_plan_no_artifacts_for_the_repository_root_combination() => _both.Artifacts.ShouldBeEmpty();
    [Fact] void should_say_why_the_combination_is_refused() => _bothMessage.Contains("RepositoryRoot", StringComparison.Ordinal).ShouldBeTrue();

    static bool IsRepositoryFile(string path) => path is ".editorconfig" or "Directory.Build.props" or "Directory.Build.targets" or "Directory.Packages.props" or "Projects.slnx" or "docker-compose.yml";

    static string Describe(IEnumerable<PlannedArtifact> artifacts, string folder) =>
        string.Join("\n", artifacts.Select(artifact => $"{artifact.RelativePath[folder.Length..]}:{ArtifactBytes(artifact)}").Order(StringComparer.Ordinal));

    static string ArtifactBytes(PlannedArtifact artifact) => Convert.ToHexString(artifact.Bytes.AsSpan());
}
