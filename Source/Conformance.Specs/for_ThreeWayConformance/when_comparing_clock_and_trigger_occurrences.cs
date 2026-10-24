// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Specifications.Semantic;
using Cratis.Stage.Specifications;
using Xunit;

namespace Cratis.Stage.Conformance.Specs.for_ThreeWayConformance;

// Independent executor parity only: live Host and rendering still refuse these occurrences.
public class when_comparing_clock_and_trigger_occurrences : Specification
{
    [Theory]
    [InlineData("Startup", "when trigger Startup", "\"one\"")]
    [InlineData("Shutdown", "when trigger Shutdown", "\"one\"")]
    [InlineData("Arrived\n          item", "when trigger Arrived\n          item = \"one\"", "item")]
    [InlineData("every 1 hour", "when clock \"2026-10-05T01:00:00Z\"", "\"one\"")]
    public async Task should_match_the_reference_occurrence_and_cascade(string trigger, string action, string destination)
    {
        var at = action.StartsWith("when clock", StringComparison.Ordinal) ? "2026-10-05T01:00:00Z" : "2026-10-05T00:30:00Z";
        var source = $$"""
            trigger Arrived
              item String
            module Work
              feature Items
                slice Automation Process
                  event Started
                    at DateTime
                  event Followed
                    at DateTime
                  reaction Worker
                    {{(trigger.StartsWith("every", StringComparison.Ordinal) ? trigger : $"when {trigger}")}}
                      produces Started
                        for {{destination}}
                        at = $context.occurred
                  reaction Follower
                    when Started
                      produces Followed
                        at = $context.occurred
                  specification Exercising
                    given clock "2026-10-05T00:30:00Z"
                    {{action}}
                    then Started
                      for "one"
                      at = "{{at}}"
                    then Followed
                      for "one"
                      at = "{{at}}"
            """;
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("AutomaticOccurrences"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("automatic"), "automatic", "Automatic.play", source);
        var compilation = new SemanticModelCompiler().Compile("AutomaticOccurrences", SemanticDocumentSet.Create([document], catalog));
        Assert.True(compilation.Success, string.Join(';', compilation.Diagnostics));
        var plan = SemanticExecutionPlan.Compile(compilation.Value.Model).Plan;
        var specification = plan.Specifications.Values.Single();
        var reference = new SemanticSpecificationRunner().Run(plan, specification.Id);
        var stage = (await new SemanticSpecificationExecutor().Run(plan, new([specification.Id]), new(), TestContext.Current.CancellationToken)).Results.Single();
        Assert.True(reference.Passed, string.Join(';', reference.Failures));
        Assert.True(stage.Outcome == SemanticSpecificationOutcome.Passed, $"{stage.Unsupported?.Details} {string.Join(';', stage.Failures)}");
        var facts = ((SemanticAccepted)reference.Execution).Facts;
        stage.Trace.Facts.Select(fact => fact.EventContract).ShouldEqual(facts.Select(fact => fact.EventContract.ToString()));
        stage.Trace.Facts.Select(fact => fact.Values.Single().Value).ShouldEqual(facts.Select(fact => $"\"{((SemanticTextValue)fact.Values.Single().Value).Value}\""));
    }
}
