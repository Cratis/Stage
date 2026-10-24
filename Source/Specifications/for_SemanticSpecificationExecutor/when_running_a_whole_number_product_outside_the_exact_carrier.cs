// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Contracts.Specifications.Semantic;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor;

public class when_running_a_whole_number_product_outside_the_exact_carrier : Specification
{
    [Fact]
    public async Task should_name_the_unsupported_product_without_escaping_the_executor()
    {
        var plan = Compile();
        var specification = plan.Specifications.Keys.Single();
        new SemanticSpecificationRunner().Run(plan, specification).Passed.ShouldBeTrue();
        var result = Assert.Single((await new SemanticSpecificationExecutor().Run(plan, new([]), new())).Results);
        result.Outcome.ShouldEqual(SemanticSpecificationOutcome.Unsupported);
        result.Unsupported!.Capability.ShouldEqual(StageExecutionCapability.Projection);
        result.Unsupported.Details.ShouldContain("Event 'LineAdded' product");
        result.Unsupported.Details.ShouldContain("exact whole-number carrier (Int64)");
        result.Failures.ShouldBeEmpty();
    }

    static SemanticExecutionPlan Compile()
    {
        const string source = """
            module Sales
              feature Lines
                slice StateChange Add
                  command AddLine
                    id String identifier
                    quantity Int
                    unitPrice Int
                    produces LineAdded
                      for id
                      quantity = quantity
                      unitPrice = unitPrice
                  event LineAdded
                    quantity Int
                    unitPrice Int
                  specification LargeProduct
                    when append LineAdded
                      for "line-one"
                      quantity = 4000000000
                      unitPrice = 4000000000
                    then readmodel Total
                      id = "line-one"
                      amount = 16000000000000000000
                slice StateView Totals
                  readmodel Total
                    id String key
                    amount Int
                  projection Build => Total
                    no automap
                    from LineAdded
                      id = $eventSourceId
                      add amount by quantity * unitPrice
            """;
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("LargeProduct"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("model"), "model", "Model.play", source);
        var compilation = new SemanticModelCompiler().Compile("LargeProduct", SemanticDocumentSet.Create([document], catalog));
        Assert.True(compilation.Success, string.Join(';', compilation.Diagnostics));
        return SemanticExecutionPlan.Compile(compilation.Value!.Model).Plan!;
    }
}
