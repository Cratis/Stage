// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_planning_a_scoped_application_with_its_user_interface;

public class with_an_omitted_owner : a_register_project_render_request
{
    ArtifactRenderPlan _plan = null!;

    void Establish()
    {
        const string siblings = """
            slice StateChange Unsupported
              command RenameOtherProject
                projectId ProjectId identifier
                toString String
                produces event OtherProjectRenamed
                  for projectId
                returns
                  projectId = projectId
                  toString = toString
            slice StateChange Unselected
              command ArchiveOtherProject
                projectId ProjectId identifier
                produces OtherProjectArchived
                  for projectId
              event OtherProjectArchived
        """;
        var form = Corpus.SourceForms.Single(candidate => candidate.Name == "single");
        var document = form.Documents.Single();
        _model = Compile(form with
        {
            Documents = [document with { Bytes = [.. Encoding.UTF8.GetBytes(document.Text + Environment.NewLine + siblings)] }]
        }).Model;
        var execution = SemanticExecutionPlan.Compile(_model);
        Assert.True(execution.Success, string.Join(Environment.NewLine, execution.Issues));
        _executionPlan = execution.Plan!;
    }

    void Because()
    {
        var refusedSlice = _model.Application.Modules.Single().Features.Single().Slices.Single(slice => slice.Name == "Unsupported");
        var profile = UserInterfaceInput.With(_request.Profile, [refusedSlice.Id.ToString(), _registerProject.Id.ToString()]);
        profile = FileHeaderInput.With(profile, ["Scoped omission header"]);
        _request = new(_model, _executionPlan, profile, new(ArtifactRenderScopeKind.Slice, refusedSlice.Id));
        _plan = new CratisArtifactRenderPlanner().Plan(_request);
    }

    [Fact] void should_report_the_refusal_and_omit_its_owner() => rendering_refusals.Rejected(_plan, "STAGE-ESM-038");

    [Fact] void should_name_the_refused_slice_in_its_user_interface_input()
    {
        UserInterfaceInput.TryRead(_request.Profile, out var slices).ShouldBeTrue();
        slices.GetValueOrDefault().ShouldContain(_model.Application.Modules.Single().Features.Single().Slices.Single(slice => slice.Name == "Unsupported").Id.ToString());
    }

    [Theory]
    [InlineData("src/bindings.ts")]
    [InlineData("src/stage.ts")]
    [InlineData("src/stage-scene.json")]
    void should_keep_only_the_selected_retained_interface(string path)
    {
        var text = Text(_plan.Artifacts.Single(artifact => artifact.RelativePath == path));
        text.ShouldContain("RegisterProject");
        text.ShouldNotContain("RenameOtherProject");
        text.ShouldNotContain(rendering_refusals.Original(_plan, "STAGE-ESM-038").Artifact.ToString());
        text.ShouldNotContain("ArchiveOtherProject");
        text.ShouldNotContain(_projectLookup.Queries.Single().Name);
    }

    [Theory]
    [InlineData("src/bindings.ts")]
    [InlineData("src/stage.ts")]
    void should_apply_the_header_after_repairing_shared_code(string path) =>
        Text(_plan.Artifacts.Single(artifact => artifact.RelativePath == path)).StartsWith("// Scoped omission header\n\n", StringComparison.Ordinal).ShouldBeTrue();

    [Fact] void should_leave_the_partial_plan_marker_header_free() =>
        Text(_plan.Artifacts.Single(artifact => artifact.RelativePath == "STAGE-NOT-RUNNABLE.md")).ShouldNotContain("Scoped omission header");
}
