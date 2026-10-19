// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Contracts.Specifications.Semantic;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor;

public class when_running_decimal_places : Specification
{
    const string Source = """
        concept Money : Decimal
          validate
            >= 0
            decimals <= 2 message "Money has at most two decimals"
        module Shop
          feature Pricing
            slice StateChange SetPrice
              command SetPrice
                productId String identifier
                price Money
                surcharge Decimal
                validate
                  surcharge decimals <= 3 message "A surcharge has at most three decimals"
                produces PriceSet
                  for productId
                  price = price
              event PriceSet
                price Money
              specification TwoDecimalsAreAccepted
                when SetPrice
                  productId = "mug"
                  price = 19.99
                  surcharge = 0.125
                then PriceSet
                  for "mug"
                  price = 19.99
              specification ATrailingZeroIsNotAPlace
                when SetPrice
                  productId = "mug"
                  price = 19.990
                  surcharge = 1
                then PriceSet
                  for "mug"
                  price = 19.99
              specification AThirdDecimalOnTheConceptIsRejected
                when SetPrice
                  productId = "mug"
                  price = 19.999
                  surcharge = 0
                then error "Money has at most two decimals"
              specification AFourthDecimalOnThePropertyIsRejected
                when SetPrice
                  productId = "mug"
                  price = 19.99
                  surcharge = 0.1255
                then error "A surcharge has at most three decimals"
        """;

    SemanticSpecificationRunReport _report = null!;
    SemanticExecutionPlan _plan = null!;

    async Task Because()
    {
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Shop"));
        var compilation = new SemanticModelCompiler().Compile("Shop", SemanticDocumentSet.Create([SemanticSourceDocument.Create(catalog.ResolveDocument("shop"), "shop", "Shop.play", Source)], catalog));
        Assert.True(compilation.Success, string.Join(';', compilation.Diagnostics));
        compilation.Value!.Model.SemanticVersion.ShouldEqual(SemanticVersion.V29);
        _plan = SemanticExecutionPlan.Compile(compilation.Value.Model).Plan!;
        _report = await new SemanticSpecificationExecutor().Run(_plan, new([]), new());
    }

    [Fact] void should_run_every_specification() => _report.Results.Count.ShouldEqual(4);
    [Fact] void should_pass_every_specification_as_the_reference_does() => _report.Results.ShouldEachConformTo(result => result.Outcome == SemanticSpecificationOutcome.Passed);
    [Fact] void should_agree_with_the_reference_evaluator() => _plan.Specifications.Values.ShouldEachConformTo(specification => new SemanticSpecificationRunner().Run(_plan, specification.Id).Passed);
}
