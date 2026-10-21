// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticWorldRebuilder.given;

public class a_locally_unadmitted_projection : a_rebuildable_world
{
    protected SemanticFact _storedFact = null!;

    void Establish()
    {
        _storedFact = ((SemanticAccepted)new SemanticEvaluator().Execute(
            _plan,
            SemanticWorld.Empty,
            SemanticExecutionRequest.Create(_command.Id, _commandValues, []) with
            {
                Occurrence = new(new DateTimeOffset(2026, 9, 24, 12, 0, 0, TimeSpan.Zero), "owner", "Owner", "owner")
            })).Facts.Single();
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Projects"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("project-model"), "project-model", "RegisterProject.play", Source + Extra);
        var compiled = new SemanticModelCompiler().Compile("Projects", SemanticDocumentSet.Create([document], catalog));
        Assert.True(compiled.Success, string.Join("; ", compiled.Diagnostics.Select(issue => issue.Message)));
        var compilation = SemanticExecutionPlan.Compile(compiled.Value!.Model, []);
        Assert.True(compilation.Success);
        Assert.Contains(compilation.UnselectedIssues, issue => issue.Artifact == compilation.Plan!.Projections.Values.Single(projection => projection.Name == "RefusedProjection").Id);
        _plan = compilation.Plan!;
    }

    protected SemanticCommand Command(string name) => _plan.Commands.Values.Single(command => command.Name == name);

    const string Extra = """

              readmodel RefusedSummary
                projectId ProjectId
                name ProjectName
              projection RefusedProjection => RefusedSummary
                remove via join on ProjectRemoved key projectId
                from ProjectRegistered key projectId
                  name = name
            slice StateChange OtherCommands
              command TouchProject
                projectId ProjectId identifier
                produces ProjectTouched
                  for projectId
                  projectId = projectId
              event ProjectTouched
                projectId ProjectId
              command StartProject
                projectId ProjectId identifier
                produces ProjectStarted
                  for projectId
                  projectId = projectId
              event ProjectStarted
                projectId ProjectId
              event ProjectRemoved
                projectId ProjectId
            slice Automation CompleteProject
              reaction CompleteProject
                when ProjectStarted
                  projectId
                  produces ProjectRegistered
                    for projectId
                    projectId = projectId
                    name = "Screenplay"
        """;
}
