// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Contracts.Semantics;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisPlanOptions.when_writing_file_headers;

public class without_a_header : a_register_project_render_request
{
    CratisPlanOptions _options_for_plan = null!;
    CratisPlanResult _scaffold = null!;
    CratisPlanResult _scoped = null!;
    ArtifactRenderPlan _application = null!;

    void Establish() => _options_for_plan = new(_model.Application.Name, "Projects", "Projects");

    void Because()
    {
        _scaffold = CratisRendering.PlanScaffold(_options_for_plan);
        _scoped = CratisRendering.PlanFrom(new LoadedSemanticModel(_model, _executionPlan), new([PlanSelectionEntry.Slice(_module.Name, _feature.Name, _registerProject.Name)]), _options_for_plan);
        _application = CratisRendering.Plan(_model, _executionPlan, new(ArtifactRenderScopeKind.Application, _model.Application.Id), _options);
    }

    [Fact] void should_default_to_no_header() => _options_for_plan.FileHeader.IsEmpty.ShouldBeTrue();
    [Fact] void should_default_the_rendering_options_to_no_header() => _options.FileHeader.IsEmpty.ShouldBeTrue();
    [Fact] void should_not_carry_a_header_in_the_profile() => CratisRendering.CreateProfile(_model.Application.Name, _options).Inputs.Any(input => input.Name == FileHeaderInput.Name).ShouldBeFalse();
    [Fact] void should_start_the_scaffold_program_with_code() => Text(_scaffold.Artifacts.Single(artifact => artifact.RelativePath == "Program.cs")).StartsWith("using ", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_start_the_slice_with_code() => Text(_scoped.Artifacts.Single(artifact => artifact.RelativePath.EndsWith("RegisterProject/RegisterProject.cs", StringComparison.Ordinal))).StartsWith("using ", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_write_no_license_header_in_the_scaffold() => _scaffold.Artifacts.Any(artifact => Text(artifact).Contains("All rights reserved", StringComparison.Ordinal)).ShouldBeFalse();
    [Fact] void should_write_no_license_header_in_the_application() => _application.Artifacts.Any(artifact => Text(artifact).Contains("All rights reserved", StringComparison.Ordinal)).ShouldBeFalse();
    [Fact] void should_start_no_application_code_file_with_a_comment() => _application.Artifacts.Any(artifact => artifact.RelativePath.EndsWith(".cs", StringComparison.Ordinal) && Text(artifact).StartsWith("//", StringComparison.Ordinal)).ShouldBeFalse();
}
