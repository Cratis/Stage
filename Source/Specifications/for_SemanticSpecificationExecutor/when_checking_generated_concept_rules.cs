// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Contracts.Specifications.Semantic;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor;

public class when_checking_generated_concept_rules : Specification
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task should_check_each_supplied_fixture_before_the_next_shape_or_missing_value(bool includeSecond)
    {
        var source = $$"""
            concept Code : String
              generate nanoid 64 alphabet "ab"
              validate
                matches "^(?!a{63}b$)[ab]{64}$" message "A different authored message"
            module M
              feature F
                slice StateChange S
                  command C
                    first Code generated
                    second Code generated
                    returns first
                  specification FailingFirst
                    when C
                      generated first = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaab"
            {{(includeSecond ? "          generated second = \"bad shape\"" : string.Empty)}}
                    then error "Generated fixture 'first' fails concept 'Code' rules"
            """;
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Rules"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("rules"), "rules", "Rules.play", source);
        var compilation = new SemanticModelCompiler().Compile("Rules", SemanticDocumentSet.Create([document], catalog));
        Assert.True(compilation.Success, string.Join(';', compilation.Diagnostics));
        var plan = SemanticExecutionPlan.Compile(compilation.Value!.Model).Plan!;
        var reference = new SemanticSpecificationRunner().Run(plan, plan.Specifications.Keys.Single());
        reference.Passed.ShouldBeTrue();
        var rejection = (SemanticRejected)reference.Execution;
        rejection.Category.ShouldEqual(SemanticRejectionCategory.Contract);
        var stage = (await new SemanticSpecificationExecutor().Run(plan, new([]), new())).Results.Single();
        stage.Outcome.ShouldEqual(SemanticSpecificationOutcome.Passed);
        stage.Trace!.Rejection.ShouldEqual(rejection.Details);
        stage.Trace.Facts.ShouldBeEmpty();
    }
}
