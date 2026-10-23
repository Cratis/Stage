// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Contracts.Specifications.Semantic;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.when_running_structured_event_values.given;

public class a_structured_event : Specification
{
    protected SemanticExecutionPlan _plan = null!;
    protected SemanticSpecificationRunRecord _result = null!;

    protected void Compile(int expected = 7, string action = "given", string member = "count = count", string facts = "", string modelProperties = "")
    {
        var occurrence = action == "command" ? "when Record\n          id = \"one\"" : $"{action} Recorded\n          for \"one\"";
        var source = $$"""
            type Details
              label String optional
              tags String[] optional
            module Work
              feature Items
                slice StateView Lookup
                  command Record
                    id String identifier
                    count Int
                    details Details
                    entries Details[]
                    numbers Int[]
                    produces Recorded
                      for id
                      count = count
                      details = details
                      entries = entries
                      numbers = numbers
                  event Recorded
                    count Int
                    details Details
                    entries Details[]
                    numbers Int[]
                  readmodel Marker
                    id String key
                    count Int
                    {{modelProperties}}
                  projection Markers => Marker
                    from Recorded
                      id = $eventSourceId
                      {{member}}
                  specification Checking
                    {{occurrence}}
                      count = 7
                      details = {"tags":[]}
                      entries = [{"label":"first"},{"tags":[]},{}]
                      numbers = [1,2]
                    then readmodel Marker
                      id = "one"
                      count = {{expected}}
                    {{facts}}
            """;
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("StructuredEvents"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("model"), "model", "Model.play", source);
        var compilation = new SemanticModelCompiler().Compile("StructuredEvents", SemanticDocumentSet.Create([document], catalog));
        Assert.True(compilation.Success, string.Join(';', compilation.Diagnostics));
        _plan = SemanticExecutionPlan.Compile(compilation.Value!.Model).Plan!;
    }

    protected async Task Execute() => _result = Assert.Single((await new SemanticSpecificationExecutor().Run(_plan, new([]), new())).Results);
    protected void AssertOutcome(SemanticSpecificationOutcome outcome) =>
        Assert.True(_result.Outcome == outcome, $"{_result.Outcome}: {_result.Unsupported?.Details} {string.Join(';', _result.Failures)}");
}
