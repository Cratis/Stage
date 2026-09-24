// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

/// <summary>
/// Verifies that admitted scoped projections are emitted in a buildable application.
/// </summary>
public class when_rendering_scoped_projections : a_generated_application
{
    internal const string ScopedSource = """
        concept ProjectId : Uuid
        concept ProjectName : String
        type ProjectNote
          noteId ProjectId
          name ProjectName
        module Projects
          feature Registration
            slice StateChange RegisterProject
              command RegisterProject
                projectId ProjectId identifier
                name ProjectName
                produces ProjectRegistered
                  for projectId
                  projectId = projectId
                  name = name
              event ProjectRegistered
                projectId ProjectId
                name ProjectName
              event ProjectRenamed
                projectId ProjectId
                name ProjectName
              event ProjectNamed
                name ProjectName
              event ProjectNoted
                noteId ProjectId
                projectId ProjectId
                name ProjectName
              specification RegisteringAProject
                when RegisterProject
                  projectId = "3fa85f64-5717-4562-b3fc-2c963f66afa6"
                  name = "Screenplay"
                then ProjectRegistered
                  projectId = "3fa85f64-5717-4562-b3fc-2c963f66afa6"
                  name = "Screenplay"
            slice StateView ProjectLookup
              readmodel ProjectSummary
                projectId ProjectId
                name ProjectName
                notes ProjectNote[]
                visits Decimal?
              query ProjectById => ProjectSummary?
                by projectId ProjectId
              query ProjectByKey => ProjectSummary?
                by projectId ProjectId
              readmodel ProjectDetails
                projectId ProjectId
                name ProjectName
              query ProjectDetailsById => ProjectDetails?
                by projectId ProjectId
              projection ProjectDetailsProjection => ProjectDetails
                from ProjectRegistered key projectId
                  name = name
              projection ProjectSummaryProjection => ProjectSummary
                from ProjectRegistered key projectId
                  name = name
                  increment visits
                from ProjectRenamed key projectId
                  name = name
                  increment visits
                join project on projectId
                  with ProjectNamed
                    no automap
                    name = name
                children notes identified by noteId
                  from ProjectNoted key noteId
                    parent projectId
                    name = name
        """;

    ArtifactRenderPlan _plan = null!;
    string _debug = null!;
    string _release = null!;
    string _specifications = null!;

    protected override ArtifactRenderPlan CreatePlan()
    {
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Projects"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("scopes"), "scopes", "Scopes.play", ScopedSource);
        var compilation = new SemanticModelCompiler().Compile("Projects", SemanticDocumentSet.Create([document], catalog));
        Assert.True(compilation.Success, string.Join(Environment.NewLine, compilation.Diagnostics));
        var model = compilation.Value!.Model;
        var execution = SemanticExecutionPlan.Compile(model);
        Assert.True(execution.Success, string.Join(Environment.NewLine, execution.Issues));
        _plan = CratisRendering.Plan(model, execution.Plan!, new(ArtifactRenderScopeKind.Application, model.Application.Id), _options);
        Assert.True(_plan.Success, string.Join(Environment.NewLine, _plan.Diagnostics));
        return _plan;
    }

    async Task Because()
    {
        _debug = await Run("scopes-debug.log", "build", "-c", "Debug");
        _release = await Run("scopes-release.log", "build", "-c", "Release");
        _specifications = await Run("scopes-specifications.log", "test", "-c", "Debug", "--no-build");
    }

    [Fact] void should_build_debug_warning_free() => BuildWarnings(_debug).ShouldEqual(string.Empty);
    [Fact] void should_build_release_warning_free() => BuildWarnings(_release).ShouldEqual(string.Empty);
    [Fact] void should_pass_the_generated_command_specification() => _specifications.ShouldContain("Passed!");
    [Fact] void should_emit_two_from_events_and_a_join() => ReadGeneratedFile("Projects/Registration/ProjectLookup/ProjectLookup.cs").ShouldContain("builder.From<ProjectRenamed>");
    [Fact] void should_emit_identified_children() => ReadGeneratedFile("Projects/Registration/ProjectLookup/ProjectLookup.cs").ShouldContain("children.IdentifiedBy(item => item.NoteId)");
    [Fact] void should_emit_increment_mappings() => ReadGeneratedFile("Projects/Registration/ProjectLookup/ProjectLookup.cs").ShouldContain("Increment(model => model.Visits)");
    [Fact] void should_emit_several_read_models_and_queries() => ReadGeneratedFile("Projects/Registration/ProjectLookup/ProjectLookup.cs").ShouldContain("ProjectDetailsById");
}
