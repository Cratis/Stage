// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Contracts.Sequences;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.Schemas;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Semantics;

namespace Cratis.Stage.Host.for_SemanticWorldRebuilder.given;

public class a_stored_typed_event : Specification
{
    protected SemanticExecutionPlan _typedPlan = null!;
    protected AppendedEventResponse _stored = null!;
    protected object? _converted;

    /// <summary>Gets the stored event names whose stored schema is exact; the event type is in it only when prepared with an exact schema.</summary>
    protected HashSet<string> _exactDecimals = [];

    /// <summary>Gets the storage evidence the prepared event type's stored schema gives.</summary>
    internal SemanticStorageEvidence Evidence => new(_exactDecimals);

    protected void Prepare(string primitive, string jsonValue, bool exactSchema = false)
    {
        var source = $$"""
            concept ProjectId : Uuid
            module Projects
              feature Registration
                slice StateChange RegisterProject
                  command RegisterProject
                    projectId ProjectId identifier
                    value {{primitive}}
                    produces ProjectRegistered
                      for projectId
                      projectId = projectId
                      value = value
                  event ProjectRegistered
                    projectId ProjectId
                    value {{primitive}}
            """;
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Projects"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("typed-event"), "typed-event", "Typed.play", source);
        var compiled = new SemanticModelCompiler().Compile("Projects", SemanticDocumentSet.Create([document], catalog));
        _typedPlan = SemanticExecutionPlan.Compile(compiled.Value.Model).Plan;
        var contract = _typedPlan.Events.Values.Single();
        var built = SemanticSchemas.Schema(contract.Properties, _typedPlan.Model.Application);
        var schema = JsonSchema.FromJson(exactSchema ? SemanticSchemas.WithDecimalFormats(JsonNode.Parse(built)!.AsObject(), null).ToJsonString() : built);
        _exactDecimals = exactSchema ? [SemanticStoredEventNames.Of(contract)] : [];
        var converter = new ExpandoObjectConverter(new TypeFormats());
        var documentJson = JsonNode.Parse($$"""{"projectId":"3fa85f64-5717-4562-b3fc-2c963f66afa6","value":{{jsonValue}}}""").AsObject();
        var expando = converter.ToExpandoObject(documentJson, schema);
        _converted = ((IDictionary<string, object?>)expando)["value"];
        _stored = new AppendedEventResponse
        {
            Content = JsonSerializer.Serialize(expando),
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
