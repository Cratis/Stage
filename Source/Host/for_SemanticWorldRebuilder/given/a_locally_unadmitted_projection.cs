// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticWorldRebuilder.given;

public class a_locally_unadmitted_projection : a_rebuildable_world
{
    protected SemanticFact _storedFact = null!;

    void Establish()
    {
        _plan = Compile(ModelSource).Plan!;
        _command = Command("RegisterProject");
        _readModel = _plan.ReadModels.Values.Single();
        _storedFact = ((SemanticAccepted)new SemanticEvaluator().Execute(
            _plan,
            SemanticWorld.Empty,
            SemanticExecutionRequest.Create(_command.Id, _commandValues, []) with
            {
                Occurrence = new(new DateTimeOffset(2026, 9, 24, 12, 0, 0, TimeSpan.Zero), "owner", "Owner", "owner")
            })).Facts.Single();
        var eventContract = _plan.Events[_storedFact.EventContract];
        _event.Content = JsonSerializer.Serialize(_storedFact.Values.ToDictionary(
            value => eventContract.Properties.Single(property => property.Id == value.TargetProperty).Name,
            value => ((SemanticTextValue)value.Value).Value));
        var compilation = Compile(ModelSource + Extra);
        _plan = compilation.Plan!;
        var refusedProjection = _plan.Projections.Values.Single(projection => projection.Name == "RefusedProjection");
        Assert.Equal(refusedProjection.Id, Assert.Single(compilation.UnselectedIssues).Artifact);
    }

    protected SemanticCommand Command(string name) => _plan.Commands.Values.Single(command => command.Name == name);

    static SemanticExecutionPlanCompilation Compile(string source)
    {
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Projects"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("project-model"), "project-model", "RegisterProject.play", source);
        var compiled = new SemanticModelCompiler().Compile("Projects", SemanticDocumentSet.Create([document], catalog));
        Assert.True(compiled.Success, string.Join("; ", compiled.Diagnostics.Select(issue => issue.Message)));
        var compilation = SemanticExecutionPlan.Compile(compiled.Value!.Model, []);
        Assert.True(compilation.Success);
        return compilation;
    }

    const string ModelSource = """
        concept ProjectId : Uuid
        concept ProjectName : String
        module Projects
          feature Registration
            slice StateChange RegisterProject
              command RegisterProject
                projectId ProjectId identifier
                name ProjectName
                produces ProjectRegistered
                  for projectId
                  name = name
              constraint UniqueProjectName
                unique name on ProjectRegistered
              event ProjectRegistered
                name ProjectName
            slice StateChange OtherCommands
              command TouchProject
                projectId ProjectId identifier
                produces ProjectTouched
                  for projectId
              event ProjectTouched
              command StartProject
                projectId ProjectId identifier
                parentId ProjectId
                produces ProjectStarted
                  for projectId
                  parentId = parentId
              event ProjectStarted
                parentId ProjectId
              event ProjectRemoved
                projectId ProjectId
            slice Automation CompleteProject
              reaction CompleteProject
                when ProjectStarted
                  parentId
                  produces ProjectRegistered
                    for parentId
                    name = "Screenplay"
            slice StateView ProjectLookup
              readmodel ProjectSummary
                projectId ProjectId
                name ProjectName
              query ProjectById => ProjectSummary optional
                by projectId ProjectId
              projection ProjectSummaryProjection => ProjectSummary
                from ProjectRegistered key $eventSourceId
                  projectId = $eventSourceId
                  name = name
        """;

    const string Extra = """

              readmodel RefusedSummary
                projectId ProjectId key
                name ProjectName
              projection RefusedProjection => RefusedSummary
                remove via join on ProjectRemoved key projectId
                from ProjectRegistered key $eventSourceId
                  projectId = $eventSourceId
                  name = name
        """;
}
