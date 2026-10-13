// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;

namespace Cratis.Stage.Host.for_SemanticWorldRebuilder.given;

public class a_stored_partial_event : Specification
{
    protected SemanticExecutionPlan _partialPlan = null!;
    protected Cratis.Chronicle.Contracts.Sequences.AppendedEventResponse _stored = null!;

    protected void Prepare(string valueType)
    {
        var source = $$"""
            concept ProjectId : Uuid
            module Projects
              feature Registration
                slice StateChange RegisterProject
                  command RegisterProject
                    projectId ProjectId identifier
                    value {{valueType}}
                    produces ProjectRegistered
                      for projectId
                      projectId = projectId
                      value = value
                  event ProjectRegistered
                    projectId ProjectId
                    value {{valueType}}
            """;
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Projects"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("partial-event"), "partial-event", "Partial.play", source);
        var compiled = new SemanticModelCompiler().Compile("Projects", SemanticDocumentSet.Create([document], catalog));
        _partialPlan = SemanticExecutionPlan.Compile(compiled.Value!.Model).Plan!;
        var contract = _partialPlan.Events.Values.Single();

        // Chronicle omits an absent value from stored content, so the stored event has no "value" property.
        _stored = new Cratis.Chronicle.Contracts.Sequences.AppendedEventResponse
        {
            Content = """{"projectId":"3fa85f64-5717-4562-b3fc-2c963f66afa6"}""",
            Context = new Cratis.Chronicle.Contracts.Sequences.EventContext
            {
                EventType = new Cratis.Chronicle.Contracts.Sequences.EventType { Id = contract.Name, Generation = 1 },
                EventSourceId = "3fa85f64-5717-4562-b3fc-2c963f66afa6",
                SequenceNumber = 0,
                EventSourceType = "Default",
                EventStreamType = "All",
                EventStreamId = "Default",
                Occurred = new() { Value = "2026-09-24T12:00:00.0000000+00:00" }
            }
        };
    }
}
