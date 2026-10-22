// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Contracts.Specifications.Semantic;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.when_running_composite_projections.given;

public class a_composite_projection : Specification
{
    protected SemanticSpecificationRunRecord _result = null!;

    protected async Task Run(string addressType = "type Address\n  street String\n  city String", string address = "{\"street\":\"Main 1\",\"city\":\"Oslo\"}", string expectedCity = "Oslo")
    {
        var source = $$"""
            {{addressType}}
            type Line
              sku String
              quantity Int
            module Work
              feature Items
                slice StateView Lookup
                  command Place
                    id String identifier
                    address Address
                    lines Line[]
                    produces Placed
                      for id
                      address = address
                      lines = lines
                  event Placed
                    address Address
                    lines Line[]
                  readmodel Order
                    id String key
                    address Address
                    lines Line[]
                    city String
                  projection Orders => Order
                    from Placed
                      id = $eventSourceId
                      address = address
                      lines = lines
                      city = address.city
                  specification Checking
                    given Placed
                      for "one"
                      address = {{address}}
                      lines = [{"sku":"A-1","quantity":2},{"sku":"B-2","quantity":1}]
                    then readmodel Order
                      id = "one"
                      city = "{{expectedCity}}"
            """;
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("CompositeProjections"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("model"), "model", "Model.play", source);
        var compilation = new SemanticModelCompiler().Compile("CompositeProjections", SemanticDocumentSet.Create([document], catalog));
        Assert.True(compilation.Success, string.Join(';', compilation.Diagnostics));
        var plan = SemanticExecutionPlan.Compile(compilation.Value!.Model).Plan!;
        _result = Assert.Single((await new SemanticSpecificationExecutor().Run(plan, new([]), new())).Results);
    }

    protected void AssertOutcome(SemanticSpecificationOutcome outcome) =>
        Assert.True(_result.Outcome == outcome, $"{_result.Outcome}: {_result.Unsupported?.Details} {string.Join(';', _result.Failures)}");
}
