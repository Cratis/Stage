// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Host.for_SemanticRuntime.given;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticSeeds.given;

public class a_seeded_model : Specification
{
    internal const string Model = """
        concept ProductName : String
        concept Price : Decimal
        module Catalog
          feature Products
            slice StateChange ListProduct
              command ListProduct
                sku String identifier
                name ProductName
                price Price
                featured Bool
                tags String[]
                produces ProductListed
                  for sku
                  name = name
                  price = price
                  featured = featured
                  tags = tags
              event ProductListed
                name ProductName
                price Price
                featured Bool
                tags String[]
            slice StateView Products
              readmodel ProductRow
                productId String
                name ProductName
                price Price
              projection ProductRows => ProductRow
                from ProductListed key $eventSourceId
                  productId = $eventSourceId
                  name = name
                  price = price
              query All => ProductRow[]
        """;

    protected SemanticExecutionPlan _plan = null!;

    protected void Compile(string seeds)
    {
        _plan = compiled_plan.From(Model);
        var result = new ScreenplayCompiler().Compile($"{Model}\n{seeds}");
        Assert.True(result.Success, string.Join(Environment.NewLine, result.Diagnostics.Select(diagnostic => $"{diagnostic.Code}: {diagnostic.Message}")));
        Syntax = result.Value!;
    }

    protected Cratis.Screenplay.Syntax.ApplicationSyntax Syntax { get; private set; } = null!;
}
