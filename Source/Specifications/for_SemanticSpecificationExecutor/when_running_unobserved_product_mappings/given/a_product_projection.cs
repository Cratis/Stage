// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Contracts.Specifications.Semantic;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.when_running_unobserved_product_mappings.given;

public class a_product_projection : Specification
{
    protected SemanticSpecificationRunRecord _result = null!;

    protected async Task Run(string givenEvent, string factorSuffix = "")
    {
        var source = $$"""
            module Work
              feature Items
                slice StateView Totals
                  command Name
                    id String identifier
                    name String
                    produces Named
                      for id
                      name = name
                  command Add
                    id String identifier
                    unitPrice Decimal
                    quantity Int
                    produces Added
                      for id
                      unitPrice = unitPrice
                      quantity = quantity
                  event Named
                    name String
                  event Added
                    unitPrice Decimal{{factorSuffix}}
                    quantity Int
                  readmodel Basket
                    id String key
                    name String optional
                    total Decimal optional
                  projection Baskets => Basket
                    no automap
                    from Named
                      name = name
                    from Added
                      add total by unitPrice * quantity
                  specification Checking
                    given {{givenEvent}}
                    then readmodel Basket
                      id = "one"
                      name = "n"
            """;
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("ProductProjections"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("model"), "model", "Model.play", source);
        var compilation = new SemanticModelCompiler().Compile("ProductProjections", SemanticDocumentSet.Create([document], catalog));
        Assert.True(compilation.Success, string.Join(';', compilation.Diagnostics));
        var plan = SemanticExecutionPlan.Compile(compilation.Value!.Model).Plan!;
        _result = Assert.Single((await new SemanticSpecificationExecutor().Run(plan, new([]), new())).Results);
    }

    protected void AssertOutcome(SemanticSpecificationOutcome outcome) =>
        Assert.True(_result.Outcome == outcome, $"{_result.Outcome}: {_result.Unsupported?.Details} {string.Join(';', _result.Failures)}");
}
