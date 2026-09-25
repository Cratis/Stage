// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text.Json;
using Cratis.Chronicle.Contracts.Sequences;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;

namespace Cratis.Stage.Host.for_SemanticWorldRebuilder.given;

public class a_rebuildable_world : Specification
{
    protected SemanticExecutionPlan _plan = null!;
    protected SemanticCommand _command = null!;
    protected AppendedEventResponse _event = null!;
    protected IReadOnlyDictionary<SemanticId, IReadOnlyList<string>> _mirror = null!;
    protected SemanticReadModel _readModel = null!;
    protected ImmutableArray<SemanticPropertyValue> _commandValues;

    void Establish()
    {
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Projects"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("project-model"), "project-model", "RegisterProject.play", Source);
        var compiled = new SemanticModelCompiler().Compile("Projects", SemanticDocumentSet.Create([document], catalog));
        _plan = SemanticExecutionPlan.Compile(compiled.Value!.Model).Plan!;
        _command = _plan.Commands.Values.Single();
        _readModel = _plan.ReadModels.Values.Single();
        _commandValues =
        [
            new(_command.Properties.Single(property => property.Name == "projectId").Id, SemanticValue.Text("3fa85f64-5717-4562-b3fc-2c963f66afa6")),
            new(_command.Properties.Single(property => property.Name == "name").Id, SemanticValue.Text("Screenplay"))
        ];
        var result = new SemanticEvaluator().Execute(
            _plan,
            SemanticWorld.Empty,
            SemanticExecutionRequest.Create(_command.Id, _commandValues, []) with
            {
                Occurrence = new(new DateTimeOffset(2026, 9, 24, 12, 0, 0, TimeSpan.Zero), "owner", "Owner", "owner")
            });
        var accepted = (SemanticAccepted)result;
        var fact = accepted.Facts.Single();
        var eventContract = _plan.Events[fact.EventContract];
        _event = new AppendedEventResponse
        {
            Context = new Cratis.Chronicle.Contracts.Sequences.EventContext
            {
                EventType = new Cratis.Chronicle.Contracts.Sequences.EventType { Id = eventContract.Name, Generation = 1 },
                EventSourceId = "3fa85f64-5717-4562-b3fc-2c963f66afa6",
                SequenceNumber = 0,
                Occurred = new() { Value = "2026-09-24T12:00:00.0000000+00:00" },
                Tags = fact.Tags
            },
            Content = JsonSerializer.Serialize(fact.Values.ToDictionary(value => eventContract.Properties.Single(property => property.Id == value.TargetProperty).Name,
                value => ((SemanticTextValue)value.Value).Value))
        };
        var values = accepted.World.ReadModels.Single().Values.ToDictionary(value => _readModel.Properties.Single(property => property.Id == value.TargetProperty).Name,
            value => ((SemanticTextValue)value.Value).Value);
        var row = JsonSerializer.Serialize(new
        {
            id = "3fa85f64-5717-4562-b3fc-2c963f66afa6",
            __initialized = true,
            projectId = values["projectId"],
            name = values["name"]
        });
        _mirror = new Dictionary<SemanticId, IReadOnlyList<string>> { [_readModel.Id] = [row] };
    }

    protected const string Source = """
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
                  projectId = projectId
                  name = name
              constraint UniqueProjectName
                unique name on ProjectRegistered
              event ProjectRegistered
                projectId ProjectId
                name ProjectName
              specification ARegisteredProject
                when RegisterProject
                  for "3fa85f64-5717-4562-b3fc-2c963f66afa6"
                  projectId = "3fa85f64-5717-4562-b3fc-2c963f66afa6"
                  name = "Screenplay"
                then ProjectRegistered
                  for "3fa85f64-5717-4562-b3fc-2c963f66afa6"
                  projectId = "3fa85f64-5717-4562-b3fc-2c963f66afa6"
                  name = "Screenplay"
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
}
