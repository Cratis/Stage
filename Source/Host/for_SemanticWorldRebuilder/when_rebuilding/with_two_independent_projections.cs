// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle;
using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Contracts.Queries;
using Cratis.Chronicle.Contracts.Sequences;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Host.for_SemanticWorldRebuilder.given;
using NSubstitute;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticWorldRebuilder.when_rebuilding;

public class with_two_independent_projections : a_rebuildable_world
{
    SemanticWorld _world = null!;
    IServices _services = null!;

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
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("two-mirrors"), "two-mirrors", "Mirrors.play", source);
        var compiled = new SemanticModelCompiler().Compile("Projects", SemanticDocumentSet.Create([document], catalog));
        Assert.True(compiled.Success, string.Join("; ", compiled.Diagnostics.Select(diagnostic => diagnostic.Message)));
        var plan = SemanticExecutionPlan.Compile(compiled.Value!.Model).Plan!;
        var accessor = Substitute.For<IChronicleServicesAccessor>();
        _services = Substitute.For<IServices>();
        accessor.Services.Returns(_services);
        _services.Sequences.TailSequenceNumber(Arg.Any<TailSequenceNumberRequest>()).Returns(
            QueryResult<EventSequenceTailResponse>.Success(Guid.Empty, new() { SequenceNumber = 1 }));
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
        _services.Sequences.FromSequenceNumber(Arg.Any<FromSequenceNumberRequest>()).Returns(
            QueryResult<IEnumerable<AppendedEventResponse>>.Success(Guid.Empty, [_event, second]));
        _world = await SemanticChronicleRegistration.Rebuild(accessor, "Projects", plan, 1);
    }

    [Fact] void should_restore_both_events() => _world.Facts.Length.ShouldEqual(2);
    [Fact] void should_rebuild_both_read_models() => _world.ReadModels.Select(instance => instance.ReadModel).Distinct().Count().ShouldEqual(2);
    [Fact] void should_not_request_mirror_projections() => _ = _services.Projections.DidNotReceiveWithAnyArgs().Register(default!);
}
