// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;

public class a_register_project_render_request : Specification
{
    protected const string Source =
        """
        concept ProjectId : Uuid
        concept ProjectName : String
        module Projects
          feature Registration
            slice StateChange RegisterProject
              command RegisterProject
                projectId ProjectId identifier
                name ProjectName
                validate
                  name not empty message "Project name is required"
                produces ProjectRegistered
                  for projectId
                  projectId = projectId
                  name = name
              event ProjectRegistered
                projectId ProjectId
                name ProjectName
              specification RegisteringAProject
                when RegisterProject
                  projectId = "3fa85f64-5717-4562-b3fc-2c963f66afa6"
                  name = "Screenplay"
                then ProjectRegistered
                  projectId = "3fa85f64-5717-4562-b3fc-2c963f66afa6"
                  name = "Screenplay"
                then readmodel ProjectSummary
                  projectId = "3fa85f64-5717-4562-b3fc-2c963f66afa6"
                  name = "Screenplay"
                then query ProjectById
                  arguments
                    projectId = "3fa85f64-5717-4562-b3fc-2c963f66afa6"
                  result
                    projectId = "3fa85f64-5717-4562-b3fc-2c963f66afa6"
                    name = "Screenplay"
              specification RejectingAnEmptyProjectName
                when RegisterProject
                  projectId = "3fa85f64-5717-4562-b3fc-2c963f66afa6"
                  name = ""
                then error "Project name is required"
            slice StateView ProjectLookup
              readmodel ProjectSummary
                projectId ProjectId
                name ProjectName
              query ProjectById => ProjectSummary?
                by projectId ProjectId
              projection ProjectSummaryProjection => ProjectSummary
                from ProjectRegistered key projectId
                  name = name
        """;

    protected CratisArtifactRenderPlanner _planner = null!;
    protected ArtifactRenderRequest _request = null!;
    protected ExecutableSemanticModel _model = null!;
    protected SemanticExecutionPlan _executionPlan = null!;
    protected CratisRenderingOptions _options = null!;
    protected SemanticModule _module = null!;
    protected SemanticFeature _feature = null!;
    protected SemanticSlice _registerProject = null!;
    protected SemanticSlice _projectLookup = null!;

    void Establish()
    {
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Projects"));
        var document = SemanticSourceDocument.Create(
            catalog.ResolveDocument("register-project-vector"),
            "register-project-vector",
            "RegisterProject.play",
            Source);
        var compilation = new SemanticModelCompiler().Compile(
            "Projects",
            SemanticDocumentSet.Create([document], catalog));
        _model = compilation.Value!.Model;
        _executionPlan = SemanticExecutionPlan.Compile(_model).Plan!;
        _options = new("Projects", "Projects");
        var profile = CratisRendering.CreateProfile(_model.Application.Name, _options);

        _module = _model.Application.Modules.Single();
        _feature = _module.Features.Single();
        _registerProject = _feature.Slices.Single(_ => _.Kind == SemanticSliceKind.StateChange);
        _projectLookup = _feature.Slices.Single(_ => _.Kind == SemanticSliceKind.StateView);
        _request = new(_model, _executionPlan, profile, new(ArtifactRenderScopeKind.Application, _model.Application.Id));
        _planner = new CratisArtifactRenderPlanner();
    }

    protected static string Text(PlannedArtifact artifact) => Encoding.UTF8.GetString(artifact.Bytes.AsSpan());
}
