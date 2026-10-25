// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Contracts.Semantics;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisPlanOptions.when_writing_file_headers;

public class with_a_multi_line_header : a_register_project_render_request
{
    const string Header = "// Copyright (c) Acme Corporation.\n//\n// Proprietary and confidential.\n\n";
    const string Slice = "Projects/Registration/RegisterProject/RegisterProject.cs";

    CratisPlanOptions _without = null!;
    CratisPlanOptions _with = null!;
    CratisPlanResult _plain_scaffold = null!;
    CratisPlanResult _scaffold = null!;
    CratisPlanResult _again = null!;
    CratisPlanResult _plain_scoped = null!;
    CratisPlanResult _scoped = null!;
    ArtifactRenderPlan _application = null!;

    void Establish()
    {
        _without = new(_model.Application.Name, "Projects", "Projects");
        _with = _without with { FileHeader = ["Copyright (c) Acme Corporation.", "   ", "Proprietary and confidential.  "] };
    }

    void Because()
    {
        var loaded = new LoadedSemanticModel(_model, _executionPlan);
        var selection = new PlanSelection([PlanSelectionEntry.Slice(_module.Name, _feature.Name, _registerProject.Name)]);
        _plain_scaffold = CratisRendering.PlanScaffold(_without);
        _scaffold = CratisRendering.PlanScaffold(_with);
        _again = CratisRendering.PlanScaffold(_with);
        _plain_scoped = CratisRendering.PlanFrom(loaded, selection, _without);
        _scoped = CratisRendering.PlanFrom(loaded, selection, _with);
        _application = CratisRendering.Plan(_model, _executionPlan, new(ArtifactRenderScopeKind.Application, _model.Application.Id), _options with { FileHeader = _with.FileHeader });
    }

    [Fact] void should_plan_the_scaffold() => _scaffold.Success.ShouldBeTrue();
    [Fact] void should_plan_the_slice() => _scoped.Success.ShouldBeTrue();
    [Fact] void should_start_the_scaffold_program_with_the_header() => Text(Artifact(_scaffold, "Program.cs")).ShouldEqual(Header + Text(Artifact(_plain_scaffold, "Program.cs")));
    [Fact] void should_start_the_frontend_entry_with_the_header() => Text(Artifact(_scaffold, ".frontend/main.tsx")).ShouldEqual(Header + Text(Artifact(_plain_scaffold, ".frontend/main.tsx")));
    [Fact] void should_start_the_vite_configuration_with_the_header() => Text(Artifact(_scaffold, ".frontend/vite.config.ts")).ShouldEqual(Header + Text(Artifact(_plain_scaffold, ".frontend/vite.config.ts")));
    [Fact] void should_start_the_slice_with_the_header() => Text(Artifact(_scoped, Slice)).ShouldEqual(Header + Text(Artifact(_plain_scoped, Slice)));
    [Fact] void should_leave_non_code_files_untouched() => _scaffold.Artifacts.Where(artifact => !IsCode(artifact)).Select(artifact => artifact.Sha256).ShouldEqual(_plain_scaffold.Artifacts.Where(artifact => !IsCode(artifact)).Select(artifact => artifact.Sha256));
    [Fact] void should_write_the_header_on_every_scaffold_code_file() => _scaffold.Artifacts.Where(IsCode).All(artifact => Text(artifact).StartsWith(Header, StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_write_the_header_on_every_application_code_file() => _application.Artifacts.Where(IsCode).All(artifact => Text(artifact).StartsWith(Header, StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_carry_the_header_in_the_profile() => CratisRendering.CreateProfile(_model.Application.Name, _options with { FileHeader = _with.FileHeader }).Inputs.Any(input => input.Name == FileHeaderInput.Name).ShouldBeTrue();
    [Fact] void should_be_deterministic() => _again.Digest.ShouldEqual(_scaffold.Digest);
    [Fact] void should_change_the_digest() => _scaffold.Digest.ShouldNotEqual(_plain_scaffold.Digest);

    static PlannedArtifact Artifact(CratisPlanResult result, string path) => result.Artifacts.Single(artifact => artifact.RelativePath == path);

    static bool IsCode(PlannedArtifact artifact) =>
        new[] { ".cs", ".ts", ".tsx", ".js", ".mjs", ".cjs" }.Any(extension => artifact.RelativePath.EndsWith(extension, StringComparison.Ordinal));
}
