// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Contracts.Specifications.Semantic;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.when_running_composite_projections.given;

public class a_composite_projection : Specification
{
    protected SemanticSpecificationRunRecord _result = null!;

    protected async Task Run(string addressType = "type Address\n  street String\n  city String", string address = "{\"street\":\"Main 1\",\"city\":\"Oslo\"}", string expectedCity = "Oslo", string lineType = "type Line\n  sku String\n  quantity Int", bool decimalPath = false, string lines = "[{\"sku\":\"A-1\",\"quantity\":2},{\"sku\":\"B-2\",\"quantity\":1}]", string? expectedLines = null, string? expectedLat = null, string? exactGiven = null, string? exactExpected = null)
    {
        var expectation = (expectedLines is null ? "" : $"\n                      lines = {expectedLines}") + (expectedLat is null ? "" : $"\n                      lat = {expectedLat}");
        var source = $$"""
            {{addressType}}
            {{lineType}}
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
                    city String{{(decimalPath ? "\n                    lat Decimal" : "")}}
                  projection Orders => Order
                    from Placed
                      id = $eventSourceId
                      address = address
                      lines = lines
                      city = address.city{{(decimalPath ? "\n                      lat = address.lat" : "")}}
                  specification Checking
                    given Placed
                      for "one"
                      address = {{address}}
                      lines = {{lines}}
                    then readmodel Order
                      id = "one"
                      city = "{{expectedCity}}"{{expectation}}
            """;
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("CompositeProjections"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("model"), "model", "Model.play", source);
        var compilation = new SemanticModelCompiler().Compile("CompositeProjections", SemanticDocumentSet.Create([document], catalog));
        Assert.True(compilation.Success, string.Join(';', compilation.Diagnostics));
        // The source parser reads every literal as a double, so a high-precision literal is restored from the text.
        var plan = exactExpected is null ? SemanticExecutionPlan.Compile(compilation.Value!.Model).Plan! : ExactNumberLiterals.Plan(compilation.Value!.Model, decimal.Parse(exactGiven!, CultureInfo.InvariantCulture), decimal.Parse(exactExpected, CultureInfo.InvariantCulture));
        _result = Assert.Single((await new SemanticSpecificationExecutor().Run(plan, new([]), new())).Results);
    }

    protected void AssertOutcome(SemanticSpecificationOutcome outcome) =>
        Assert.True(_result.Outcome == outcome, $"{_result.Outcome}: {_result.Unsupported?.Details} {string.Join(';', _result.Failures)}");
}
