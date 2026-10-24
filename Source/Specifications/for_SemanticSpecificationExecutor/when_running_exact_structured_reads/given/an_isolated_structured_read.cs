// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Contracts.Specifications.Semantic;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.when_running_exact_structured_reads.given;

/// <summary>A projection holding exactly one structured mapping: a nested leaf read or a whole-composite copy.</summary>
public class an_isolated_structured_read : Specification
{
    protected SemanticSpecificationRunRecord _result = null!;

    protected async Task Run(string memberType, string value, bool wholeCopy, string? exactExpected = null)
    {
        var mapping = wholeCopy ? "detail = detail" : "amount = detail.amount";
        var property = wholeCopy ? "detail Detail" : $"amount {memberType}";
        var expectation = wholeCopy ? $"\n                      detail = {{\"amount\":{value}}}" : $"\n                      amount = {value}";
        var source = $$"""
            type Detail
              amount {{memberType}}
            module Work
              feature Items
                slice StateView Lookup
                  command Place
                    id String identifier
                    detail Detail
                    produces Placed
                      for id
                      detail = detail
                  event Placed
                    detail Detail
                  readmodel Order
                    id String key
                    {{property}}
                  projection Orders => Order
                    no automap
                    from Placed
                      id = $eventSourceId
                      {{mapping}}
                  specification Checking
                    given Placed
                      for "one"
                      detail = {"amount":{{value}}}
                    then readmodel Order
                      id = "one"{{expectation}}
            """;
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("ExactReads"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("model"), "model", "Model.play", source);
        var compilation = new SemanticModelCompiler().Compile("ExactReads", SemanticDocumentSet.Create([document], catalog));
        Assert.True(compilation.Success, string.Join(';', compilation.Diagnostics));
        // The source parser reads every literal as a double, so a high-precision literal is restored from the text.
        var plan = exactExpected is null ? SemanticExecutionPlan.Compile(compilation.Value!.Model).Plan! : ExactNumberLiterals.Plan(compilation.Value!.Model, decimal.Parse(value, CultureInfo.InvariantCulture), decimal.Parse(exactExpected, CultureInfo.InvariantCulture));
        _result = Assert.Single((await new SemanticSpecificationExecutor().Run(plan, new([]), new())).Results);
    }

    protected void AssertOutcome(SemanticSpecificationOutcome outcome) =>
        Assert.True(_result.Outcome == outcome, $"{_result.Outcome}: {_result.Unsupported?.Details} {string.Join(';', _result.Failures)}");
}
