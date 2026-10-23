// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Contracts.Specifications.Semantic;
using Cratis.Stage.Semantics;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.when_running_product_mappings.given;

public class a_product_run : Specification
{
    protected SemanticExecutionPlan _plan = null!;
    protected SemanticSpecificationRunRecord _result = null!;
    protected bool _referencePassed;
    protected string _referenceFailures = string.Empty;

    /// <summary>
    /// Compiles a basket model, runs the reference and the executor.
    /// </summary>
    /// <param name="priceType">The type of the unit price.</param>
    /// <param name="quantityType">The type of the quantity, with an optional marker when it has one.</param>
    /// <param name="price">The unit price literal.</param>
    /// <param name="quantity">The quantity literal.</param>
    /// <param name="total">The total the specification asserts.</param>
    protected async Task Run(string priceType, string quantityType, string price, string quantity, string total)
    {
        var source = $$"""
            module Work
              feature Items
                slice StateView Totals
                  command Add
                    id String identifier
                    unitPrice {{priceType}}
                    quantity {{quantityType}}
                    produces Added
                      for id
                      unitPrice = unitPrice
                      quantity = quantity
                  event Added
                    unitPrice {{priceType}}
                    quantity {{quantityType}}
                  readmodel Basket
                    id String key
                    total {{(priceType == "Decimal" || quantityType.StartsWith("Decimal") ? "Decimal" : "Int")}} optional
                  projection Baskets => Basket
                    no automap
                    from Added
                      add total by unitPrice * quantity
                  specification Checking
                    given Added
                      for "one"
                      unitPrice = {{price}}
                      quantity = {{quantity}}
                    then readmodel Basket
                      id = "one"
                      total = {{total}}
            """;
        _plan = when_running_property_keyed_removal_only_projections.Compile(source);
        var reference = new SemanticSpecificationRunner().Run(_plan, _plan.Specifications.Keys.Single());
        _referencePassed = reference.Passed;
        _referenceFailures = string.Join(';', reference.Failures);
        _result = Assert.Single((await new SemanticSpecificationExecutor().Run(_plan, new([]), new())).Results);
    }

    protected void AssertParity(bool passes)
    {
        Assert.True(_referencePassed == passes, $"Reference passed {_referencePassed}: {_referenceFailures}");
        var expected = passes ? SemanticSpecificationOutcome.Passed : SemanticSpecificationOutcome.Failed;
        Assert.True(_result.Outcome == expected, $"{_result.Outcome}: {_result.Unsupported?.Details} {string.Join(';', _result.Failures)}");
    }

    protected void AssertRefused(string detail)
    {
        Assert.True(_result.Outcome == SemanticSpecificationOutcome.Unsupported, $"{_result.Outcome}: {string.Join(';', _result.Failures)}");
        Assert.Contains(detail, _result.Unsupported!.Details);
    }
}
