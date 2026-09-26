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
/// Builds and executes the Chronicle 19.8 projection shapes that Stage now admits.
/// </summary>
public class when_executing_re_admitted_scoped_projections : a_generated_application
{
    const string Probe = """
        // Copyright (c) Cratis. All rights reserved.
        // Licensed under the MIT license. See LICENSE file in the project root for full license information.

        #if DEBUG
        using Cratis.Chronicle.Events;
        using Cratis.Chronicle.Testing.ReadModels;
        using Projects.Common;
        using Projects.Projects.Registration.RegisterProject;
        using Projects.Projects.Registration.ProjectLookup;
        using Xunit;

        namespace Projects.Projects.Registration;

        public class when_replaying_re_admitted_scopes
        {
            const string First = "3fa85f64-5717-4562-b3fc-2c963f66afa6";
            const string Second = "4fa85f64-5717-4562-b3fc-2c963f66afa7";
            const string Note = "5fa85f64-5717-4562-b3fc-2c963f66afa8";

            [Fact]
            public async Task should_project_safe_literals_on_a_from_event()
            {
                var literal = new ReadModelScenario<ProjectSummary>();
                await literal.Given.ForEventSource(new EventSourceId(First)).Events(
                    new ProjectRegistered(new ProjectId(Guid.Parse(First)), new ProjectName("First")));
                var projected = literal.InstanceForEventSourceId(new EventSourceId(First));
                Assert.Equal("fixed", projected?.Label);
                Assert.Equal(Guid.Parse(Second), projected?.FixedId);
                Assert.Equal(12.5m, projected?.FixedCount);
                Assert.Equal(new DateOnly(2026, 9, 26), projected?.FixedDate);
            }

            [Fact]
            public async Task should_remove_a_joined_child_from_both_parents()
            {
                var scenario = new ReadModelScenario<ProjectSummary>();
                await scenario.Given.ForEventSource(new EventSourceId(First)).Events(
                    new ProjectRegistered(new ProjectId(Guid.Parse(First)), new ProjectName("First")));
                await scenario.Given.ForEventSource(new EventSourceId(Second)).Events(
                    new ProjectRegistered(new ProjectId(Guid.Parse(Second)), new ProjectName("Second")));
                await scenario.Given.ForEventSource(new EventSourceId(Note)).Events(
                    new ProjectNoted(new ProjectId(Guid.Parse(Note)), new ProjectId(Guid.Parse(First)), new ProjectName("A")),
                    new ProjectNoted(new ProjectId(Guid.Parse(Note)), new ProjectId(Guid.Parse(Second)), new ProjectName("B")));
                Assert.Single(scenario.InstanceForEventSourceId(new EventSourceId(First))!.Notes);
                Assert.Single(scenario.InstanceForEventSourceId(new EventSourceId(Second))!.Notes);
                await scenario.Given.ForEventSource(new EventSourceId(Note)).Events(
                    new ProjectNoteRemovedViaJoin(new ProjectId(Guid.Parse(Note))));
                Assert.Empty(scenario.InstanceForEventSourceId(new EventSourceId(First))!.Notes);
                Assert.Empty(scenario.InstanceForEventSourceId(new EventSourceId(Second))!.Notes);
            }
        }
        #endif
        """;

    ArtifactRenderPlan _plan = null!;
    string _debug = null!;
    string _release = null!;
    string _tests = null!;

    protected override ArtifactRenderPlan CreatePlan()
    {
        var source = when_rendering_scoped_projections.ScopedSource
            .Replace("notes ProjectNote[]", "label String?\n        fixedId Uuid?\n        fixedCount Decimal?\n        fixedDate Date?\n        notes ProjectNote[]", StringComparison.Ordinal)
            .Replace("increment visits", "label = \"fixed\"\n          fixedId = \"4fa85f64-5717-4562-b3fc-2c963f66afa7\"\n          fixedCount = 12.5\n          fixedDate = \"2026-09-26\"\n          increment visits", StringComparison.Ordinal)
            .Replace("remove with ProjectNoteRemoved key noteId\n            parent projectId", "remove with ProjectNoteRemoved key noteId\n            parent projectId\n          remove via join on ProjectNoteRemovedViaJoin key noteId", StringComparison.Ordinal);
        Assert.Contains("label String?", source, StringComparison.Ordinal);
        Assert.Contains("label = \"fixed\"", source, StringComparison.Ordinal);
        Assert.Contains("remove via join on ProjectNoteRemovedViaJoin", source, StringComparison.Ordinal);
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Projects"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("scopes"), "scopes", "Scopes.play", source);
        var compilation = new SemanticModelCompiler().Compile("Projects", SemanticDocumentSet.Create([document], catalog));
        Assert.True(compilation.Success, string.Join(Environment.NewLine, compilation.Diagnostics));
        var model = compilation.Value!.Model;
        var execution = SemanticExecutionPlan.Compile(model);
        Assert.True(execution.Success, string.Join(Environment.NewLine, execution.Issues));
        _plan = CratisRendering.Plan(model, execution.Plan!, new(ArtifactRenderScopeKind.Application, model.Application.Id), new("Projects", "Projects"));
        Assert.True(_plan.Success, string.Join(Environment.NewLine, _plan.Diagnostics));
        return _plan;
    }

    async Task Because()
    {
        AddGeneratedSpecification("Projects/Registration/when_replaying_re_admitted_scopes.cs", Probe);
        _debug = await Run("readmission-debug.log", "build", "-c", "Debug", "-warnaserror");
        _release = await Run("readmission-release.log", "build", "-c", "Release", "-warnaserror");
        _tests = await Run("readmission-test.log", "test", "-c", "Debug", "--no-build");
    }

    [Fact] void should_build_and_execute_every_re_admitted_projection()
    {
        BuildWarnings(_debug).ShouldEqual(string.Empty);
        BuildWarnings(_release).ShouldEqual(string.Empty);
        _tests.ShouldContain("Passed!");
        var code = ReadGeneratedFile("Projects/Registration/ProjectLookup/ProjectLookup.cs");
        code.ShouldContain("ToValue(\"fixed\")");
        code.ShouldContain("children.RemovedWithJoin<ProjectNoteRemovedViaJoin>");
    }
}
#endif
