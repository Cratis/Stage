// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle;
using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Contracts.Observation;
using Cratis.Chronicle.Contracts.Queries;
using Cratis.Chronicle.Contracts.Sequences;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Host.for_SemanticWorldRebuilder.given;
using Cratis.Stage.Semantics;
using NSubstitute;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticWorldRebuilder.when_rebuilding;

public class with_two_independent_mirrors : a_rebuildable_world
{
    SemanticWorld _world = null!;
    string[] _filters = null!;

    async Task Because()
    {
        const string source = Source + "\n" + """
                slice StateChange RenameProject
                  command RenameProject
                    projectId ProjectId identifier
                    name ProjectName
                    produces ProjectRenamed
                      for projectId
                      projectId = projectId
                      name = name
                  event ProjectRenamed
                    projectId ProjectId
                    name ProjectName
                slice StateView RenamedLookup
                  readmodel RenamedSummary
                    projectId ProjectId
                    name ProjectName
                  query RenamedById => RenamedSummary?
                    by projectId ProjectId
                  projection RenamedProjection => RenamedSummary
                    from ProjectRenamed key projectId
                      name = name
            """;
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Projects"));
        var document = SemanticSourceDocument.Create(
            catalog.ResolveDocument("two-mirrors"), "two-mirrors", "Mirrors.play", source);
        var compiled = new SemanticModelCompiler().Compile("Projects", SemanticDocumentSet.Create([document], catalog));
        Assert.True(compiled.Success, string.Join("; ", compiled.Diagnostics.Select(diagnostic => diagnostic.Message)));
        var plan = SemanticExecutionPlan.Compile(compiled.Value!.Model).Plan!;
        var accessor = Substitute.For<IChronicleServicesAccessor>();
        var services = Substitute.For<IServices>();
        accessor.Services.Returns(services);
        var mirrors = plan.Projections.Values.Select(projection =>
        {
            SemanticProjectionMirrors.TryLower(plan, projection, out var model, out var definition, out _);
            return (Projection: projection, Model: model!, Definition: definition!);
        }).ToArray();
        services.Observers.GetObservers(Arg.Any<AllObserversRequest>()).Returns([.. mirrors.Select(mirror => new ObserverInformation
        {
            Id = mirror.Definition.Identifier,
            IsSubscribed = true,
            RunningState = Cratis.Chronicle.Contracts.Observation.ObserverRunningState.Active,
            LastHandledEventSequenceNumber = mirror.Projection.Name == "ProjectSummaryProjection" ? 0UL : 1UL
        })]);
        var requests = new List<string>();
        services.Sequences.TailSequenceNumber(Arg.Any<TailSequenceNumberRequest>()).Returns(call =>
        {
            var filter = call.Arg<TailSequenceNumberRequest>().EventTypeIds;
            if (filter is not null)
            {
                requests.Add(filter);
            }

            var sequence = filter?.Contains("ProjectRenamed", StringComparison.Ordinal) != false ? 1UL : 0UL;
            return Task.FromResult(QueryResult<EventSequenceTailResponse>.Success(Guid.Empty, new() { SequenceNumber = sequence }));
        });
        var second = new AppendedEventResponse
        {
            Context = new Cratis.Chronicle.Contracts.Sequences.EventContext
            {
                EventType = new Cratis.Chronicle.Contracts.Sequences.EventType { Id = plan.Events.Values.Single(@event => @event.Name.Contains("ProjectRenamed", StringComparison.Ordinal)).Name, Generation = 1 },
                EventSourceId = _event.Context.EventSourceId,
                SequenceNumber = 1,
                Occurred = _event.Context.Occurred,
                Tags = []
            },
            Content = _event.Content
        };
        services.Sequences.FromSequenceNumber(Arg.Any<FromSequenceNumberRequest>()).Returns(QueryResult<IEnumerable<AppendedEventResponse>>.Success(Guid.Empty, [_event, second]));
        services.FailedPartitions.GetFailedPartitions(Arg.Any<GetFailedPartitionsRequest>()).Returns([]);
        services.ReadModels.GetInstances(Arg.Any<Cratis.Chronicle.Contracts.ReadModels.GetInstancesRequest>()).Returns(_ =>
        {
            return new Cratis.Chronicle.Contracts.ReadModels.GetInstancesResponse
            {
                TotalCount = 1,
                Instances = [_mirror.Values.Single().Single()]
            };
        });
        _world = await SemanticChronicleRegistration.Rebuild(accessor, "Projects", plan, 1);
        _filters = [.. requests];
    }

    [Fact] void should_restore_both_events() => _world.Facts.Length.ShouldEqual(2);
    [Fact] void should_request_each_mirrors_event_tail() => _filters.Length.ShouldEqual(2);
    [Fact] void should_filter_each_projection_to_its_own_event() => _filters.Distinct().Count().ShouldEqual(2);
}
