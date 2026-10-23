// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;

namespace Cratis.Stage.Specifications.Types.for_SemanticRuntimeTypes.given;

public class a_structured_event_contract : Specification
{
    protected SemanticExecutionPlan _plan = null!;
    internal SemanticRuntimeTypes _types = null!;
    protected Type _eventType = null!;

    void Establish()
    {
        var source = """
            type Details
              label String
              amount Decimal
              count Int
              flag Bool
              reference Uuid
              day Date
              moment DateTime
            type Node
              value Decimal
              next Node optional
            module Work
              feature Items
                slice StateChange Record
                  command Record
                    id String identifier
                    details Details
                    lines Details[]
                    tags String[]
                    amounts Decimal[]
                    chain Node
                    produces Recorded
                      for id
                      details = details
                      lines = lines
                      tags = tags
                      amounts = amounts
                      chain = chain
                  event Recorded
                    details Details
                    lines Details[]
                    tags String[]
                    amounts Decimal[]
                    chain Node
            """;
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("StructuredEventTypes"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("model"), "model", "Model.play", source);
        var compilation = new SemanticModelCompiler().Compile("StructuredEventTypes", SemanticDocumentSet.Create([document], catalog));
        Assert.True(compilation.Success, string.Join(';', compilation.Diagnostics));
        _plan = SemanticExecutionPlan.Compile(compilation.Value!.Model).Plan!;
        _types = new SemanticRuntimeTypes(_plan);
        _eventType = _types.For(_plan.Events.Keys.Single());
    }

    protected Type Shape(string property) => _eventType.GetProperty(property)!.PropertyType.GetCustomAttributes(typeof(Cratis.Chronicle.Schemas.JsonSchemaTypeAttribute), false).Cast<Cratis.Chronicle.Schemas.JsonSchemaTypeAttribute>().Single().Type;
}
