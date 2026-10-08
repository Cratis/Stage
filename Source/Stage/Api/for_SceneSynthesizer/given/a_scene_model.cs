// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay;
using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Cratis.Stage.Contracts;
using Cratis.Stage.Contracts.Scene;
using Cratis.Stage.Contracts.Screenplay;

namespace Cratis.Stage.Api.for_SceneSynthesizer.given;

public class a_scene_model : Specification
{
    protected EventModel _eventModel = null!;
    protected ExecutableSemanticModel _semanticModel = null!;
    protected SceneApplication _scene = null!;

    void Establish()
    {
        var syntax = new ScreenplayCompiler().Compile(Source);
        syntax.Success.ShouldBeTrue();
        _eventModel = new ScreenplayEventModelVisitor().Visit(syntax.Value!);
        _scene = new ScreenplaySceneVisitor().Visit(syntax.Value!);
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Projects"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("model"), "model", "model.play", Source);
        var compilation = new SemanticModelCompiler().Compile("Projects", SemanticDocumentSet.Create([document], catalog));
        compilation.Success.ShouldBeTrue();
        _semanticModel = compilation.Value!.Model;
    }

    public const string Source = """
        concept ProjectId : Uuid
        concept ProjectName : String
        concept ProjectStage : Enum
          planned
          active
        module Projects
          feature Portfolio
            feature Registration
              slice StateChange RegisterProject
                command RegisterProject
                  projectId ProjectId identifier
                  name ProjectName
                  stage ProjectStage
                  description String?
                  tags String[]
                  validate
                    name not empty message "Project name is required"
                  produces ProjectRegistered
                    for projectId
                    projectId = projectId
                    name = name
                    stage = stage
                event ProjectRegistered
                  projectId ProjectId
                  name ProjectName
                  stage ProjectStage
              slice StateView ProjectLookup
                readmodel ProjectSummary
                  projectId ProjectId
                  name ProjectName
                  stage ProjectStage
                query ProjectById => ProjectSummary?
                  by projectId ProjectId
                projection ProjectSummaryProjection => ProjectSummary
                  from ProjectRegistered key projectId
                    name = name
        """;
}
