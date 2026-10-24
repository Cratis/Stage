// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Contracts.Specifications.Semantic;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor;

public class when_running_ordinal_text_rules : Specification
{
    [Theory]
    [InlineData("contains", "ABC", "abc")]
    [InlineData("not contains", "abc", "ABC")]
    [InlineData("starts with", "ABC", "abc")]
    [InlineData("ends with", "ABC", "abc")]
    public async Task should_execute_accepted_and_rejected_command_rules_in_v24(string spelling, string accepted, string rejected)
    {
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("TextRules"));
        var source = $$"""
            module TextRules
              feature Checking
                slice StateChange Choose
                  command Choose
                    reference String
                    validate
                      reference {{spelling}} "ABC"
                    returns reference
                  specification Accepted
                    when Choose
                      reference = "{{accepted}}"
                    then returns "{{accepted}}"
                  specification Rejected
                    when Choose
                      reference = "{{rejected}}"
                    then error
            """;
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("model"), "model", "Model.play", source);
        var compilation = new SemanticModelCompiler().Compile("TextRules", SemanticDocumentSet.Create([document], catalog));
        Assert.True(compilation.Success, string.Join(Environment.NewLine, compilation.Diagnostics));
        var model = compilation.Value.Model;
        model.SemanticVersion.ShouldEqual(SemanticVersion.V24);
        var plan = SemanticExecutionPlan.Compile(model).Plan;
        var report = await new SemanticSpecificationExecutor().Run(plan, new([]), new(), TestContext.Current.CancellationToken);
        report.Results.Count.ShouldEqual(2);
        report.Results.ShouldEachConformTo(result => result.Outcome == SemanticSpecificationOutcome.Passed);
    }
}
