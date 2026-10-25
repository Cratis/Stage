// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Text;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Cratis.Stage.Rendering.Cratis.for_CratisRenderer;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_planning_same_named_commands_in_distinct_slices : a_register_project_render_request
{
    const string OriginalPath = "Projects/Registration/RegisterProject/RegisterProject.cs";
    const string AdditionalPath = "Projects/Registration/RegisterAgain/RegisterAgain.cs";

    ArtifactRenderPlan _plan = null!;
    RenderedFile[] _sources = null!;

    void Establish()
    {
        var form = Corpus.SourceForms.Single(_ => _.Name == "single");
        var original = form.Documents.Single();
        const string AdditionalSlice = """
                slice StateChange RegisterAgain
                  command RegisterProject
                    projectId ProjectId identifier
                    name ProjectName
                    produces ProjectRegistered
                      for projectId
                      projectId = projectId
                      name = name
            """;
        var document = original with
        {
            Bytes = [.. Encoding.UTF8.GetBytes(original.Text + Environment.NewLine + AdditionalSlice)]
        };
        var compilation = Compile(form with { Documents = [document] });
        compilation.Model.ShouldNotBeNull();
        _model = compilation.Model;
        var execution = SemanticExecutionPlan.Compile(_model);
        execution.Success.ShouldBeTrue();
        execution.Plan.ShouldNotBeNull();
        _executionPlan = execution.Plan!;

        var feature = _model.Application.Modules.Single().Features.Single();
        var originalSlice = feature.Slices.Single(_ => _.Name == "RegisterProject");
        var additional = feature.Slices.Single(_ => _.Name == "RegisterAgain");
        originalSlice.Id.Equals(additional.Id).ShouldBeFalse();
        originalSlice.Commands.Single().Name.ShouldEqual("RegisterProject");
        additional.Commands.Single().Name.ShouldEqual("RegisterProject");
        originalSlice.Commands.Single().Id.Equals(additional.Commands.Single().Id).ShouldBeFalse();
    }

    void Because()
    {
        _plan = CratisRendering.Plan(
            _model,
            _executionPlan,
            new(ArtifactRenderScopeKind.Application, _model.Application.Id),
            _options);
        _sources =
        [
            .. _plan.Artifacts
                .Where(_ => _.RelativePath.EndsWith(".cs", StringComparison.Ordinal) && _.RelativePath != "Program.cs")
                .Select(_ => new RenderedFile(_.RelativePath, Text(_)))
        ];
    }

    [Fact] void should_be_publishable() => _plan.Success.ShouldBeTrue();
    [Fact] void should_not_report_a_command_name_collision() => _plan.Diagnostics.Select(_ => _.Code).ShouldNotContain("STAGE-ESM-011");
    [Fact] void should_render_the_original_slice() => _plan.Artifacts.Any(_ => _.RelativePath == OriginalPath).ShouldBeTrue();
    [Fact] void should_render_the_additional_slice() => _plan.Artifacts.Any(_ => _.RelativePath == AdditionalPath).ShouldBeTrue();
    [Fact] void should_keep_the_original_slice_namespace() => Content(OriginalPath).ShouldContain("namespace Projects.Projects.Registration.RegisterProject;");
    [Fact] void should_use_a_distinct_namespace_for_the_additional_slice() => Content(AdditionalPath).ShouldContain("namespace Projects.Projects.Registration.RegisterAgain;");
    [Fact] void should_render_the_same_command_name_in_the_original_slice() => Content(OriginalPath).ShouldContain("public record RegisterProject(ProjectId ProjectId, ProjectName Name)");
    [Fact] void should_render_the_same_command_name_in_the_additional_slice() => Content(AdditionalPath).ShouldContain("public record RegisterProject(ProjectId ProjectId, ProjectName Name)");
    [Fact] void should_produce_nonempty_generated_sources() => _sources.ShouldNotBeEmpty();
    [Fact] void should_compile_the_generated_backend_and_specifications() => string.Join(Environment.NewLine, RenderedOutput.Errors(_sources)).ShouldEqual(string.Empty);

    string Content(string path) => Text(_plan.Artifacts.Single(_ => _.RelativePath == path));
}
#endif
