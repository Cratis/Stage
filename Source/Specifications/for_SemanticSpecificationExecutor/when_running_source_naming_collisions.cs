// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Contracts.Specifications.Semantic;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor;

public class when_running_source_naming_collisions : Specification
{
    [Theory]
    [InlineData("Text", "ById")]
    [InlineData("Record", "Total")]
    public async Task should_execute_and_agree_with_the_reference(string commandName, string queryName)
    {
        var source = $$"""
            module Payments
              feature Recording
                slice StateChange RecordPayment
                  command {{commandName}}
                    id String identifier
                    text String
                    produces Recorded
                      for id
                      text = text
                  event Recorded
                    text String
                  specification Recording
                    when {{commandName}}
                      id = "payment-1"
                      text = "paid"
                    then Recorded
                      text = "paid"
                    then readmodel PaymentTotals
                      id = "payment-1"
                      total = "paid"
                slice StateView Lookup
                  readmodel PaymentTotals
                    id String key
                    total String
                  query {{queryName}} => PaymentTotals?
                    by id String
                  projection Build => PaymentTotals
                    from Recorded
                      id = $eventSourceId
                      total = text
            """;
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Payments"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("names"), "names", "Names.play", source);
        var compilation = new SemanticModelCompiler().Compile("Payments", SemanticDocumentSet.Create([document], catalog));
        Assert.True(compilation.Success, string.Join(';', compilation.Diagnostics));
        var execution = SemanticExecutionPlan.Compile(compilation.Value!.Model);
        Assert.True(execution.Success, string.Join(';', execution.Issues));
        var plan = execution.Plan!;
        var specification = plan.Specifications.Values.Single();
        var reference = new SemanticSpecificationRunner().Run(plan, specification.Id);

        var result = Assert.Single((await new SemanticSpecificationExecutor().Run(plan, new([specification.Id]), new())).Results);

        Assert.True(reference.Passed, string.Join(';', reference.Failures));
        Assert.True(result.Outcome == SemanticSpecificationOutcome.Passed, $"{result.Outcome}: {result.Unsupported?.Details} {string.Join(';', result.Failures)}");
        result.ExecutionKind.ShouldEqual(reference.Execution.Kind.ToString());
    }
}
