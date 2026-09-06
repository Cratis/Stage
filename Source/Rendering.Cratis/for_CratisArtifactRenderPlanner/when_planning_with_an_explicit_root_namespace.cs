// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Cratis.Stage.Rendering.Cratis.for_CratisRenderer;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_planning_with_an_explicit_root_namespace : a_register_project_render_request
{
    ArtifactRenderPlan _plan = null!;
    ArtifactRenderPlan _repeated = null!;
    ArtifactRenderPlan _renamedProject = null!;
    ArtifactRenderPlan[] _scopes = null!;
    RenderedFile[] _sources = null!;

    void Establish() => _options = new("BackendHost", "Acme.projectAPI");

    void Because()
    {
        _plan = CratisRendering.Plan(_model, _executionPlan, _request.Scope, _options);
        _repeated = CratisRendering.Plan(_model, _executionPlan, _request.Scope, _options);
        _renamedProject = CratisRendering.Plan(_model, _executionPlan, _request.Scope, _options with { ProjectName = "OtherHost" });
        _scopes =
        [
            CratisRendering.Plan(_model, _executionPlan, new(ArtifactRenderScopeKind.Module, _module.Id), _options),
            CratisRendering.Plan(_model, _executionPlan, new(ArtifactRenderScopeKind.Feature, _feature.Id), _options),
            CratisRendering.Plan(_model, _executionPlan, new(ArtifactRenderScopeKind.Slice, _registerProject.Id), _options)
        ];
        _sources = [.. _plan.Artifacts.Where(IsSemanticSource).Select(_ => new RenderedFile(_.RelativePath, Text(_)))];
    }

    [Fact] void should_admit_the_explicit_options() => _plan.Success.ShouldBeTrue();
    [Fact] void should_name_the_project_independently() => _plan.Artifacts.Any(_ => _.RelativePath == "BackendHost.csproj").ShouldBeTrue();
    [Fact] void should_use_the_exact_root_namespace_in_the_project() => Text(_plan.Artifacts.Single(_ => _.RelativePath == "BackendHost.csproj")).ShouldContain("<RootNamespace>Acme.projectAPI</RootNamespace>");
    [Fact] void should_render_every_source_and_specification_in_the_exact_namespace() => _sources.All(_ => _.Content.Contains("namespace Acme.projectAPI.", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_import_concepts_from_the_exact_namespace() => _sources.Single(_ => _.RelativePath.EndsWith("/RegisterProject.cs", StringComparison.Ordinal)).Content.ShouldContain("using Acme.projectAPI.Common;");
    [Fact] void should_import_cross_slice_events_from_the_exact_namespace() => _sources.Single(_ => _.RelativePath.EndsWith("/ProjectLookup.cs", StringComparison.Ordinal)).Content.ShouldContain("using Acme.projectAPI.Projects.Registration.RegisterProject;");
    [Fact] void should_compile_the_generated_sources_and_debug_specifications() => string.Join(Environment.NewLine, RenderedOutput.Errors(_sources)).ShouldEqual(string.Empty);
    [Fact] void should_compile_without_warnings() => string.Join(Environment.NewLine, RenderedOutput.Warnings(_sources)).ShouldEqual(string.Empty);
    [Fact] void should_preserve_semantic_bytes_across_project_names() => _renamedProject.Artifacts.Where(IsSemanticSource).All(MatchesApplication).ShouldBeTrue();
    [Fact] void should_admit_nested_scopes() => _scopes.All(_ => _.Success).ShouldBeTrue();
    [Fact] void should_preserve_all_selected_source_and_specification_bytes_in_nested_scopes() => _scopes.SelectMany(_ => _.Artifacts).All(MatchesApplication).ShouldBeTrue();
    [Fact] void should_repeat_exact_artifacts() => _repeated.Artifacts.Zip(_plan.Artifacts).All(_ => _.First.RelativePath == _.Second.RelativePath && _.First.Sha256 == _.Second.Sha256 && _.First.Bytes.SequenceEqual(_.Second.Bytes)).ShouldBeTrue();
    [Fact] void should_repeat_the_artifact_count() => _repeated.Artifacts.Length.ShouldEqual(_plan.Artifacts.Length);

    static bool IsSemanticSource(PlannedArtifact artifact) => artifact.RelativePath.EndsWith(".cs", StringComparison.Ordinal) && artifact.RelativePath != "Program.cs";
    bool MatchesApplication(PlannedArtifact artifact) => artifact.Bytes.SequenceEqual(_plan.Artifacts.Single(_ => _.RelativePath == artifact.RelativePath).Bytes);
}
#endif
