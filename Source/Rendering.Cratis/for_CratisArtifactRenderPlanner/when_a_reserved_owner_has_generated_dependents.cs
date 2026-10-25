// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.Scene;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_a_reserved_owner_has_generated_dependents : given.a_register_project_render_request
{
    ArtifactRenderPlan _plan = null!;

    void Establish()
    {
        var application = _model.Application with
        {
            Modules =
            [
                _module with { Features = [_feature with { Slices = [_projectLookup] }] },
                _module with
                {
                    Id = SemanticId.Create(SemanticKind.Module, "reserved-module"),
                    Name = "Customizations",
                    Features = [_feature with { Id = SemanticId.Create(SemanticKind.Feature, "reserved-feature"), Slices = [_registerProject] }]
                }
            ]
        };
        _model = ExecutableSemanticModel.Create(_model.LanguageVersion, _model.SemanticVersion, application);
        _request = new(_model, SemanticExecutionPlan.Compile(_model).Plan!, _request.Profile, new(ArtifactRenderScopeKind.Application, application.Id));
    }

    void Because() => _plan = _planner.Plan(_request);

    [Fact] void should_keep_the_partial_plan_successful() => _plan.Outcome.ShouldEqual(ArtifactRenderOutcome.Partial);
    [Fact] void should_omit_the_projection_that_consumes_the_omitted_event() => _plan.Artifacts.Any(artifact => artifact.Sources.Contains(_projectLookup.Id)).ShouldBeFalse();
    [Fact] void should_record_the_dependent_owner_gap() => _plan.ImplementationGaps.Any(gap => gap.Owner == _projectLookup.Id.ToString() && gap.Kind == ArtifactImplementationGapKind.Rendering && gap.RequirementId == $"render:STAGE-CRATIS-005:{_projectLookup.Id}").ShouldBeTrue();
    [Fact] void should_remove_the_command_proxy_import() => Text(_plan.Artifacts.Single(artifact => artifact.RelativePath == StageModuleRenderer.RelativePath)).ShouldNotContain(_registerProject.Commands.Single().Name);
    [Fact] void should_remove_the_query_proxy_import() => Text(_plan.Artifacts.Single(artifact => artifact.RelativePath == SceneBindingsRenderer.RelativePath)).ShouldNotContain(_projectLookup.Queries.Single().Name);
    [Fact] void should_remove_the_scene_command_reference() => Text(_plan.Artifacts.Single(artifact => artifact.RelativePath == StageSceneDocument.RelativePath)).ShouldNotContain(_registerProject.Commands.Single().Name);
    [Fact] void should_remove_the_scene_query_reference() => Text(_plan.Artifacts.Single(artifact => artifact.RelativePath == SceneCompositionInput.RelativePath)).ShouldNotContain(_projectLookup.Queries.Single().Name);
    [Fact] void should_keep_independent_concepts() => _plan.Artifacts.Any(artifact => artifact.RelativePath.StartsWith("Common/", StringComparison.Ordinal) && artifact.RelativePath.EndsWith(".cs", StringComparison.Ordinal)).ShouldBeTrue();
#if DEBUG
    [Fact] void should_compile_the_remaining_backend() => given.opaque_policy_model.Compile(_plan).GetTypes().Length.ShouldBeGreaterThan(0);
#endif
}
