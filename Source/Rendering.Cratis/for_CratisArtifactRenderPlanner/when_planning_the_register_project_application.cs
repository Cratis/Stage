// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Cratis.Stage.Rendering.Cratis.for_CratisRenderer;
using FluentValidation;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_planning_the_register_project_application : a_register_project_render_request
{
    ArtifactRenderPlan _first = null!;
    ArtifactRenderPlan _second = null!;
    IReadOnlyList<string> _compilationErrors = null!;
    int _validationErrors;

    void Because()
    {
        _first = _planner.Plan(_request);
        _second = _planner.Plan(_request);
        var sources = _first.Artifacts
            .Where(_ => _.RelativePath.EndsWith(".cs", StringComparison.Ordinal))
            .Select(_ => new RenderedFile(_.RelativePath, Text(_)))
            .ToArray();
        _compilationErrors = RenderedOutput.Errors(sources);
        _validationErrors = ValidateEmptyName(RenderedOutput.Load(sources));
    }

    [Fact] void should_be_publishable() => _first.Success.ShouldBeTrue();
    [Fact] void should_include_the_resolved_scaffold() => _first.Artifacts.Any(_ => _.RelativePath == "Projects.csproj").ShouldBeTrue();
    [Fact] void should_render_the_identifier_concept() => Content("Common/ProjectId.cs").ShouldContain("EventSourceId<Guid>");
    [Fact] void should_render_the_value_concept() => Content("Common/ProjectName.cs").ShouldContain("ConceptAs<string>");
    [Fact] void should_render_the_command_from_esm() => Content("Projects/Registration/RegisterProject/RegisterProject.cs").ShouldContain("public record RegisterProject(ProjectId ProjectId, ProjectName Name)");
    [Fact] void should_render_the_exact_validation_message() => Content("Projects/Registration/RegisterProject/RegisterProject.cs").ShouldContain("WithMessage(\"Project name is required\")");
    [Fact] void should_render_the_event_destination_as_the_identifier() => Content("Projects/Registration/RegisterProject/RegisterProject.cs").ShouldContain("public ProjectRegistered Handle() => new(ProjectId, Name);");
    [Fact] void should_render_the_one_instance_projection() => Content("Projects/Registration/ProjectLookup/ProjectLookup.cs").ShouldContain("[FromEvent<ProjectRegistered>]");
    [Fact] void should_render_the_optional_snapshot_query() => Content("Projects/Registration/ProjectLookup/ProjectLookup.cs").ShouldContain("public static Task<ProjectSummary?> ProjectById");
    [Fact] void should_render_the_success_command_specification() => ContentEnding("when_registering_aproject.cs").ShouldContain("ShouldHaveAppendedEvent<RegisterProject, ProjectRegistered>");
    [Fact] void should_render_the_rejection_specification() => ContentEnding("when_rejecting_an_empty_project_name.cs").ShouldContain("ShouldHaveValidationErrors");
    [Fact] void should_render_the_projection_specification() => ContentEnding("when_registering_aproject_is_projected.cs").ShouldContain("ReadModelScenario<ProjectSummary>");
    [Fact] void should_render_the_query_specification() => ContentEnding("when_registering_aproject_is_queried.cs").ShouldContain("ProjectSummary.ProjectById");
    [Fact] void should_compile_the_generated_backend_and_specifications() => string.Join(Environment.NewLine, _compilationErrors).ShouldEqual(string.Empty);
    [Fact] void should_reject_the_empty_project_name() => _validationErrors.ShouldEqual(1);
    [Fact] void should_repeat_the_same_artifact_paths() => _second.Artifacts.Select(_ => _.RelativePath).SequenceEqual(_first.Artifacts.Select(_ => _.RelativePath)).ShouldBeTrue();
    [Fact] void should_repeat_the_same_artifact_hashes() => _second.Artifacts.Select(_ => _.Sha256).SequenceEqual(_first.Artifacts.Select(_ => _.Sha256)).ShouldBeTrue();
    [Fact] void should_repeat_the_same_artifact_bytes() => _second.Artifacts.Zip(_first.Artifacts).All(pair => pair.First.Bytes.SequenceEqual(pair.Second.Bytes)).ShouldBeTrue();

    string Content(string path) => Text(_first.Artifacts.Single(_ => _.RelativePath == path));
    string ContentEnding(string path) => Text(_first.Artifacts.Single(_ => _.RelativePath.EndsWith(path, StringComparison.Ordinal)));

    static int ValidateEmptyName(System.Reflection.Assembly assembly)
    {
        var projectId = Activator.CreateInstance(assembly.GetTypes().Single(_ => _.Name == "ProjectId"), Guid.NewGuid());
        var projectName = Activator.CreateInstance(assembly.GetTypes().Single(_ => _.Name == "ProjectName"), string.Empty);
        var commandType = assembly.GetTypes().Single(_ => _.Name == "RegisterProject");
        var command = Activator.CreateInstance(commandType, projectId, projectName)!;
        var validator = (IValidator)Activator.CreateInstance(assembly.GetTypes().Single(_ => _.Name == "RegisterProjectValidator"))!;
        var contextType = typeof(ValidationContext<>).MakeGenericType(commandType);
        var context = (IValidationContext)Activator.CreateInstance(contextType, command)!;
        return validator.Validate(context).Errors.Count;
    }
}
