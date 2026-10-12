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

namespace Cratis.Stage.Host.for_SemanticWorldRebuilder.given;

public class a_stored_enumeration_event : Specification
{
    protected SemanticExecutionPlan _plan = null!;
    protected AppendedEventResponse _stored = null!;
    protected object? _converted;

    protected void Prepare(string jsonValue, bool numbered = false)
    {
        const string source = """
            concept InvoiceId : String
            concept Status : Enum
              draft
              issued
            module Billing
              feature Invoicing
                slice StateChange Issue
                  command IssueInvoice
                    id InvoiceId identifier
                    status Status
                    produces InvoiceIssued
                      for id
                      status = status
                  event InvoiceIssued
                    status Status
            """;
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Invoices"));
        var declarations = numbered ? source.Replace("  draft\n  issued", "  draft = -1\n  issued = 90", StringComparison.Ordinal) : source;
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("enumeration-event"), "enumeration-event", "Invoices.play", declarations);
        var compiled = new SemanticModelCompiler().Compile("Invoices", SemanticDocumentSet.Create([document], catalog));
        _plan = SemanticExecutionPlan.Compile(compiled.Value!.Model).Plan!;
        var contract = _plan.Events.Values.Single();

        // The rendered application's Chronicle schema, not the Host's current string schema (Stage#310).
        var schema = JsonSchema.FromJson(numbered
            ? """{"type":"object","properties":{"status":{"type":"integer","enum":[-1,90],"x-enumNames":["Draft","Issued"]}}}"""
            : """{"type":"object","properties":{"status":{"type":"integer","enum":[0,1],"x-enumNames":["Draft","Issued"]}}}""");
        var converter = new ExpandoObjectConverter(new TypeFormats());
        var content = converter.ToExpandoObject(JsonNode.Parse($$"""{"status":{{jsonValue}}}""")!.AsObject(), schema);
        _converted = ((IDictionary<string, object?>)content)["status"];
        _stored = new AppendedEventResponse
        {
            Content = JsonSerializer.Serialize(content),
            Context = new Cratis.Chronicle.Contracts.Sequences.EventContext
            {
                EventType = new Cratis.Chronicle.Contracts.Sequences.EventType { Id = contract.Name, Generation = 1 },
                EventSourceId = "invoice-one",
                SequenceNumber = 0,
                EventSourceType = "Default",
                EventStreamType = "All",
                EventStreamId = "Default",
                Occurred = new() { Value = "2026-09-24T12:00:00.0000000+00:00" }
            }
        };
    }

    // Exercise historical names or corrupt content that Chronicle's integer converter would not store.
    protected void UseStoredValue(string jsonValue) => _stored.Content = $$"""{"status":{{jsonValue}}}""";
}
