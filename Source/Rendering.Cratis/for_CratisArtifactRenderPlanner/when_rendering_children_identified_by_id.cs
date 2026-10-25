// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

/// <summary>
/// A child Id is a collection identity, not Chronicle's root document key.
/// </summary>
public class when_rendering_children_identified_by_id : a_generated_application
{
    protected override ArtifactRenderPlan CreatePlan()
    {
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Projects"));
        var source = when_rendering_scoped_projections.ScopedSource.Replace("noteId", "id", StringComparison.Ordinal);
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("child-id"), "child-id", "ChildId.play", source);
        var compilation = new SemanticModelCompiler().Compile("Projects", SemanticDocumentSet.Create([document], catalog));
        Assert.True(compilation.Success, string.Join("; ", compilation.Diagnostics.Select(diagnostic => diagnostic.Message)));
        var model = compilation.Value!.Model;
        var execution = SemanticExecutionPlan.Compile(model);
        Assert.True(execution.Success, string.Join("; ", execution.Issues));
        var plan = CratisRendering.Plan(model, execution.Plan!, new(ArtifactRenderScopeKind.Application, model.Application.Id), new("Projects", "Projects"));
        Assert.True(plan.Success, string.Join("; ", plan.Diagnostics));
        return plan;
    }

    string _tests = null!;

    async Task Because()
    {
        AddGeneratedSpecification("Projects/Registration/ProjectLookup/when_a_child_has_id.cs", """
            // Copyright (c) Cratis. All rights reserved.
            // Licensed under the MIT license. See LICENSE file in the project root for full license information.

            #if DEBUG
            using Cratis.Chronicle.Events;
            using Cratis.Chronicle.Testing.ReadModels;
            using Projects.Common;
            using Projects.Projects.Registration.RegisterProject;
            using Xunit;

            namespace Projects.Projects.Registration.ProjectLookup;

            public class when_a_child_has_id
            {
                [Fact]
                public async Task should_keep_the_child_identity_distinct_from_the_document_key()
                {
                    var parentId = Guid.Parse("3fa85f64-5717-4562-b3fc-2c963f66afa6");
                    var childId = Guid.Parse("5fa85f64-5717-4562-b3fc-2c963f66afa8");
                    var parent = new EventSourceId(parentId.ToString());
                    var child = new EventSourceId(childId.ToString());
                    var scenario = new ReadModelScenario<ProjectSummary>();
                    await scenario.Given.ForEventSource(parent).Events(new ProjectRegistered(new ProjectId(parentId), new ProjectName("Parent")));
                    await scenario.Given.ForEventSource(child).Events(new ProjectNoted(new ProjectId(childId), new ProjectId(parentId), new ProjectName("Note")));
                    var instance = Assert.IsType<ProjectSummary>(scenario.InstanceForEventSourceId(parent));
                    Assert.Equal(parentId, instance.ProjectId.Value);
                    var note = Assert.Single(instance.Notes!);
                    Assert.Equal(childId, note.Id.Value);
                    Assert.Equal("Note", note.Name.Value);
                }
            }
            #endif
            """);
        await Run("child-id-build.log", "build", "-c", "Debug", "-warnaserror");
        _tests = await Run("child-id-tests.log", "test", "-c", "Debug", "--no-build");
    }

    [Fact] void should_run_the_generated_child_projection() => _tests.ShouldContain("Passed!");
    [Fact] void should_render_id_as_a_child_member() => ReadGeneratedFile("Projects/Registration/ProjectLookup/ProjectLookup.cs").ShouldContain("children.IdentifiedBy(item => item.Id)");
}
#endif
