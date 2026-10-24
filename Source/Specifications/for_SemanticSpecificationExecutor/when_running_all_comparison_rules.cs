// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Contracts.Specifications.Semantic;
using Cratis.Stage.Semantics;
using Cratis.Stage.Specifications.Commands;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor;

public class when_running_all_comparison_rules : Specification
{
    [Theory]
    [InlineData(">", "Int", "[1, 2]", "[1, 0]", "error")]
    [InlineData(">=", "Decimal", "[0, 0.5]", "[0.5, -0.1]", "warning")]
    [InlineData(">", "Count", "[1, 2]", "[1, 0]", "information")]
    [InlineData(">=", "Amount", "[0, 0.5]", "[0.5, -0.1]", "error")]
    public async Task should_execute_every_collection_outcome(string comparison, string type, string accepted, string rejected, string severity)
    {
        var source = $$"""
            concept Count : Int
            concept Amount : Decimal
            module Comparisons
              feature Checking
                slice StateChange Check
                  command Check
                    values {{type}}[] optional
                    answer Bool
                    validate
                      values all {{comparison}} 0 severity {{severity}} message "All values must meet the bound"
                    returns answer
                  specification Accepted
                    when Check
                      values = {{accepted}}
                      answer = true
                    then returns true
                  specification Rejected
                    when Check
                      values = {{rejected}}
                      answer = true
                    then error "All values must meet the bound"
                  specification Empty
                    when Check
                      values = []
                      answer = true
                    then returns true
            """;
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Comparisons"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("model"), "model", "Model.play", source);
        var compilation = new SemanticModelCompiler().Compile("Comparisons", SemanticDocumentSet.Create([document], catalog));
        Assert.True(compilation.Success, string.Join(Environment.NewLine, compilation.Diagnostics));
        var plan = SemanticExecutionPlan.Compile(compilation.Value.Model).Plan;
        foreach (var specification in plan.Specifications.Values)
        {
            SemanticRunAdmission.Check(plan, specification).ShouldBeNull();
            var reference = new SemanticSpecificationRunner().Run(plan, specification.Id);
            Assert.True(reference.Passed, string.Join(';', reference.Failures));
        }
        var command = plan.Commands.Values.Single();
        var rejectedSpecification = plan.Specifications.Values.Single(specification => specification.Name == "Rejected");
        var failure = SemanticRuleEvaluation.RuleFailures(command, rejectedSpecification, plan.Model.Application).Single();
        failure.Message.ShouldEqual("All values must meet the bound");
        failure.Severity.ShouldEqual(command.Validations.Single().Severity);
        var report = await new SemanticSpecificationExecutor().Run(plan, new([]), new(), TestContext.Current.CancellationToken);
        report.Results.Count.ShouldEqual(3);
        report.Results.ShouldEachConformTo(result => result.Outcome == SemanticSpecificationOutcome.Passed);
    }
}
