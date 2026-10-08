// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;
using Xunit.Abstractions;

using static Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.ThreeWayOutcomes;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

/// <summary>
/// Exercises re-admitted projection sequences through the three-way specification harness.
/// Exact nested snapshots are covered by the scoped projection differential vectors: generated
/// specifications cannot assert composite values, and Stage's executor still refuses these scopes.
/// </summary>
public class when_comparing_re_admitted_projection_shapes(when_comparing_re_admitted_projection_shapes.context fixture, ITestOutputHelper output) : IClassFixture<when_comparing_re_admitted_projection_shapes.context>
{
    [Fact] void should_compare_all_three_sequences() => fixture.Outcomes.Length.ShouldEqual(3);
    [Fact] void should_match_every_executed_path() => Report(fixture, output);
    [Fact] void should_report_the_stage_executor_projection_gap() => Assert.All(fixture.Outcomes, outcome => Assert.Contains("Reference=Passed, Stage=Unsupported(Projection), Rendered=Passed", outcome, StringComparison.Ordinal));

    public class context : a_three_way_application
    {
        const string First = "3fa85f64-5717-4562-b3fc-2c963f66afa6";
        const string Second = "4fa85f64-5717-4562-b3fc-2c963f66afa7";
        const string Note = "5fa85f64-5717-4562-b3fc-2c963f66afa8";
        const string Source = """
            concept ProjectId : Uuid
            concept ProjectName : String
            type ProjectInfo
              name ProjectName
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
                  command RenameProject
                    projectId ProjectId identifier
                    name ProjectName
                    produces ProjectRenamed
                      for projectId
                      projectId = projectId
                      name = name
                  command NoteProject
                    noteId ProjectId identifier
                    projectId ProjectId
                    name ProjectName
                    produces ProjectNoted
                      for noteId
                      noteId = noteId
                      projectId = projectId
                      name = name
                  command RemoveProjectNote
                    noteId ProjectId identifier
                    produces ProjectNoteRemovedViaJoin
                      for noteId
                      noteId = noteId
                  event ProjectRegistered
                    projectId ProjectId
                    name ProjectName
                  event ProjectRenamed
                    projectId ProjectId
                    name ProjectName
                  event ProjectNoted
                    noteId ProjectId
                    projectId ProjectId
                    name ProjectName
                  event ProjectNoteRemovedViaJoin
                    noteId ProjectId
                slice StateView ProjectLookup
                  readmodel ProjectSummary
                    projectId ProjectId
                    name ProjectName
                    info ProjectInfo?
                    notes ProjectNote[]
                  query ProjectById => ProjectSummary?
                    by projectId ProjectId
                  projection ProjectSummaryProjection => ProjectSummary
                    no automap
                    from ProjectRegistered key projectId
                      name = name
                    from ProjectRenamed
                      name = name
                    nested info
                      from ProjectRegistered key projectId
                        name = name
                      clear with ProjectRenamed
                    children notes identified by noteId
                      from ProjectNoted key noteId
                        parent projectId
                        name = name
                      remove via join on ProjectNoteRemovedViaJoin key noteId
            """;

        protected override string ApplicationName => "Projects";
        protected override string ProjectFile => "BackendHost.csproj";
        protected override IEnumerable<(string Form, ExecutableSemanticModel Model)> Models => [("re-admitted-scopes", CompileSequences())];
        Task Because() => Verify();

        static ExecutableSemanticModel CompileSequences()
        {
            var recreation = $$"""
                  specification RecreatingNestedObject
                    given ProjectRegistered
                      for "{{First}}"
                      projectId = "{{First}}"
                      name = "First"
                    given ProjectRenamed
                      for "{{First}}"
                      projectId = "{{First}}"
                      name = "Updated"
                    when RegisterProject
                      projectId = "{{First}}"
                      name = "Again"
                    then ProjectRegistered
                      projectId = "{{First}}"
                      name = "Again"
                    then readmodel ProjectSummary
                      projectId = "{{First}}"
                      name = "Again"
                """;
            var joined = new[] { ("First", First), ("Second", Second) }.Select(parent => $$"""
                  specification RemovingJoinedChildFrom{{parent.Item1}}Parent
                    given ProjectRegistered
                      for "{{First}}"
                      projectId = "{{First}}"
                      name = "First"
                    given ProjectRegistered
                      for "{{Second}}"
                      projectId = "{{Second}}"
                      name = "Second"
                    given ProjectNoted
                      for "{{Note}}"
                      noteId = "{{Note}}"
                      projectId = "{{First}}"
                      name = "A"
                    given ProjectNoted
                      for "{{Note}}"
                      noteId = "{{Note}}"
                      projectId = "{{Second}}"
                      name = "B"
                    given ProjectNoteRemovedViaJoin
                      for "{{Note}}"
                      noteId = "{{Note}}"
                    when RegisterProject
                      projectId = "{{parent.Item2}}"
                      name = "{{parent.Item1}}"
                    then ProjectRegistered
                      projectId = "{{parent.Item2}}"
                      name = "{{parent.Item1}}"
                    then readmodel ProjectSummary
                      projectId = "{{parent.Item2}}"
                      notes = []
                """);
            var specifications = string.Join('\n', new[] { recreation }.Concat(joined)).Split('\n').Select(line => "    " + line);
            var source = Source.Replace("    slice StateView ProjectLookup", string.Join('\n', specifications) + "\n    slice StateView ProjectLookup", StringComparison.Ordinal);
            var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Projects"));
            var document = SemanticSourceDocument.Create(catalog.ResolveDocument("re-admitted"), "re-admitted", "Scopes.play", source);
            var compilation = new SemanticModelCompiler().Compile("Projects", SemanticDocumentSet.Create([document], catalog));
            Assert.True(compilation.Success, string.Join(Environment.NewLine, compilation.Diagnostics));
            return compilation.Value!.Model;
        }
    }
}
#endif
