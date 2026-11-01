// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Screenplay.Semantics.Serialization;
using Cratis.Stage.Contracts.Specifications.Semantic;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor;

public class when_running_anonymous_policies : Specification
{
    [Fact]
    public async Task should_refuse_an_anonymous_denial_without_executing_it()
    {
        var model = SemanticModelSerializer.Deserialize(AnonymousPoliciesCorpus.V31.EsmBytes.AsSpan());
        var denied = model.Application.Modules.SelectMany(module => module.Features).SelectMany(feature => feature.Slices)
            .SelectMany(slice => slice.Specifications).First(specification => specification.When is not null);
        typeof(SemanticSpecification).GetProperty(nameof(SemanticSpecification.GivenCaller)).SetValue(denied, new SemanticCaller(true, [], []));
        typeof(SemanticSpecification).GetProperty(nameof(SemanticSpecification.ThenDenied)).SetValue(denied, true);
        typeof(SemanticSpecification).GetProperty(nameof(SemanticSpecification.ThenEvents)).SetValue(denied, denied.ThenEvents.Clear());
        typeof(SemanticSpecification).GetProperty(nameof(SemanticSpecification.ThenQueries)).SetValue(denied, denied.ThenQueries.Clear());
        typeof(SemanticSpecification).GetProperty(nameof(SemanticSpecification.ThenReadModels)).SetValue(denied, denied.ThenReadModels.Clear());
        var plan = SemanticExecutionPlan.Compile(model).Plan;
        var observed = 0;
        var executor = new SemanticSpecificationExecutor { ObserveWorldFacts = (_, _) => observed++ };
        var result = (await executor.Run(plan, new([denied.Id]), new())).Results.Single();
        result.Outcome.ShouldEqual(SemanticSpecificationOutcome.Unsupported);
        result.Unsupported.Details.ShouldContain("STAGE-ESM-011");
        observed.ShouldEqual(0);
    }

    [Fact]
    public async Task should_accept_visitors_and_signed_in_customers_with_the_reference_fact_counts()
    {
        var corpus = AnonymousPoliciesCorpus.V31;
        var model = SemanticModelSerializer.Deserialize(corpus.EsmBytes.AsSpan());
        var plan = SemanticExecutionPlan.Compile(model).Plan!;
        plan.Specifications.Values.ShouldContain(specification => specification.GivenCaller is null);
        plan.Specifications.Values.ShouldContain(specification => specification.GivenCaller is { Authenticated: true });
        var factCounts = new Dictionary<SemanticId, int>();
        var executor = new SemanticSpecificationExecutor { ObserveWorldFacts = (id, count) => factCounts.Add(id, count) };
        var report = await executor.Run(plan, new([]), new());
        report.Results.Count.ShouldEqual(corpus.SpecificationExpectations.Length);
        foreach (var expectation in corpus.SpecificationExpectations)
        {
            var result = report.Results.Single(result => result.SpecificationId == expectation.Specification.ToString());
            Assert.True(result.Outcome == SemanticSpecificationOutcome.Passed,
                $"{expectation.Name}: {result.Outcome} {result.Unsupported?.Details} {string.Join(';', result.Failures)}");
            result.ExecutionKind.ShouldEqual(expectation.Outcome.ToString());
            factCounts[expectation.Specification].ShouldEqual(expectation.WorldFactCount!.Value);
        }
    }
}
