// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_planning_a_composite_type : a_register_project_render_request
{
    ArtifactRenderPlan _plan = null!;

    void Because()
    {
        var source = Source.Replace(
            "module Projects",
            """
            type ProjectMetadata
              name ProjectName
            module Projects
            """,
            StringComparison.Ordinal);
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Projects"));
        var document = SemanticSourceDocument.Create(
            catalog.ResolveDocument("register-project-with-composite"),
            "register-project-with-composite",
            "RegisterProject.play",
            source);
        var compilation = new SemanticModelCompiler().Compile(
            "Projects",
            SemanticDocumentSet.Create([document], catalog));
        var model = compilation.Value!.Model;
        var request = new ArtifactRenderRequest(
            model,
            SemanticExecutionPlan.Compile(model).Plan!,
            _request.Profile,
            new(ArtifactRenderScopeKind.Application, model.Application.Id));
        _plan = _planner.Plan(request);
    }

    [Fact] void should_be_publishable() => _plan.Success.ShouldBeTrue();
    [Fact] void should_render_the_composite_type() =>
        Text(_plan.Artifacts.Single(_ => _.RelativePath == "Common/ProjectMetadata.cs"))
            .ShouldContain("public record ProjectMetadata(ProjectName Name);");
}
