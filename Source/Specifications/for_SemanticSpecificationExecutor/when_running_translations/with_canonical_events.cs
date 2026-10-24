// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Screenplay.Semantics.Serialization;
using Cratis.Stage.Contracts.Specifications.Semantic;
using Cratis.Stage.Specifications.Commands;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.when_running_translations;

public class with_canonical_events : Specification
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task should_match_the_frozen_reference_outcomes_and_facts(bool captures)
    {
        var corpus = captures ? PublicEventsCorpus.CapturesFromEventsV9 : PublicEventsCorpus.V9;
        var plan = SemanticExecutionPlan.Compile(SemanticModelSerializer.Deserialize(corpus.EsmBytes.AsSpan())).Plan;
        var observed = new Dictionary<SemanticId, int>();
        var executor = new SemanticSpecificationExecutor { ObserveWorldFacts = (id, count) => observed[id] = count };
        var report = await executor.Run(plan, new([.. plan.Specifications.Keys]), new(), TestContext.Current.CancellationToken);
        report.Results.Count.ShouldEqual(corpus.SpecificationExpectations.Length);
        foreach (var specification in plan.Specifications.Values)
        {
            var expected = corpus.SpecificationExpectations.Single(candidate => candidate.Name == specification.Name);
            var reference = new SemanticSpecificationRunner().Run(plan, specification.Id);
            var actual = report.Results.Single(result => result.SpecificationId == specification.Id.ToString());
            Assert.True(actual.Outcome == SemanticSpecificationOutcome.Passed, $"{specification.Name}: {actual.Unsupported?.Details} {string.Join(';', actual.Failures)}");
            reference.Passed.ShouldBeTrue();
            actual.ExecutionKind.ShouldEqual(reference.Execution.Kind.ToString());
            reference.Execution.Kind.ShouldEqual(expected.Outcome);
            observed[specification.Id].ShouldEqual(reference.Execution.World.Facts.Length);
            reference.Execution.World.Facts.Length.ShouldEqual(expected.WorldFactCount ?? 2);
            var accepted = (SemanticAccepted)reference.Execution;
            actual.Trace.Facts.Select(fact => fact.EventContract).ShouldEqual(accepted.Facts.Select(fact => fact.EventContract.ToString()));
            foreach (var pair in accepted.Facts.Zip(actual.Trace.Facts))
            {
                pair.Second.EventSource.ShouldEqual(SemanticRunContext.Canonical(pair.First.Destination));
                foreach (var value in pair.First.Values)
                {
                    pair.Second.Values[value.TargetProperty.ToString()].ShouldEqual(SemanticRunContext.Canonical(value.Value));
                }
            }
        }
    }
}
