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
        using Projects.Projects.Registration.AllLookup;
        using Xunit;

        namespace Projects.Projects.Registration;

        public class when_replaying_re_admitted_scopes
        {
            const string First = "3fa85f64-5717-4562-b3fc-2c963f66afa6";
            const string Second = "4fa85f64-5717-4562-b3fc-2c963f66afa7";
            const string Note = "5fa85f64-5717-4562-b3fc-2c963f66afa8";

            [Fact]
            public async Task should_keep_one_all_mapping_for_a_from_event_and_observe_an_unrelated_event()
            {
                var literal = new ReadModelScenario<ProjectSummary>();
                await literal.Given.ForEventSource(new EventSourceId(First)).Events(
                    new ProjectRegistered(new ProjectId(Guid.Parse(First)), new ProjectName("First")));
                Assert.Equal("fixed", literal.InstanceForEventSourceId(new EventSourceId(First))?.Label);
                var scenario = new ReadModelScenario<AllSummary>();
                await scenario.Given.ForEventSource(new EventSourceId(First)).Events(
                    new ProjectRegistered(new ProjectId(Guid.Parse(First)), new ProjectName("First")));
                var from = scenario.InstanceForEventSourceId(new EventSourceId(First));
                Assert.NotNull(from);
                Assert.Equal(Guid.Parse(First), from.LastSeen?.Value);
                await scenario.Given.ForEventSource(new EventSourceId(Second)).Events(new ProjectInfoCleared());
                var unrelated = scenario.InstanceForEventSourceId(new EventSourceId(Second));
                Assert.NotNull(unrelated);
                Assert.Equal(Guid.Parse(Second), unrelated.LastSeen?.Value);
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
            .Replace("notes ProjectNote[]", "label String?\n        notes ProjectNote[]", StringComparison.Ordinal)
            .Replace("increment visits", "label = \"fixed\"\n          increment visits", StringComparison.Ordinal)
            .Replace("remove with ProjectNoteRemoved key noteId\n            parent projectId", "remove with ProjectNoteRemoved key noteId\n            parent projectId\n          remove via join on ProjectNoteRemovedViaJoin key noteId", StringComparison.Ordinal) + "\n" + """
                slice StateView AllLookup
                  readmodel AllSummary
                    projectId ProjectId
                    lastSeen ProjectId?
                  query AllById => AllSummary?
                    by projectId ProjectId
                  projection AllSummaryProjection => AllSummary
                    from ProjectRegistered key projectId
                    all
                      lastSeen = $eventSourceId
            """;
        Assert.Contains("all\n", source, StringComparison.Ordinal);
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
        ReadGeneratedFile("Projects/Registration/AllLookup/AllLookup.cs").ShouldContain("builder.FromAll(every =>");
        code.ShouldContain("ToValue(\"fixed\")");
        code.ShouldContain("children.RemovedWithJoin<ProjectNoteRemovedViaJoin>");
    }
}
#endif
