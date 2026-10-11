// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Contracts.Specifications.Semantic;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor;

public class when_running_generated_values_and_responses : given.a_command_only_plan
{
    readonly List<SemanticSpecificationRunRecord> _results = [];
    protected override CanonicalCorpusVector Corpus => RegisterProjectCorpus.V7;

    async Task Because()
    {
        var expected = _specification;
        var wrong = expected with
        {
            ThenReturns = expected.ThenReturns switch
            {
                SemanticScalarSpecificationResponse scalar => scalar with { Value = SemanticValue.Text("22222222-2222-2222-2222-222222222222") },
                SemanticRecordSpecificationResponse record => record with { Fields = [.. record.Fields.Select(field => field with { Value = SemanticValue.Text("22222222-2222-2222-2222-222222222222") })] },
                _ => throw new MissingResponseFixture()
            }
        };
        var missing = expected with { When = expected.When! with { GeneratedValues = [] } };
        foreach (var specification in new[] { expected, wrong, missing })
        {
            _results.Add((await new SemanticSpecificationExecutor().Run(With(specification), new([specification.Id]), new())).Results.Single());
        }
    }

    [Fact] void should_execute_a_pinned_response() => _results[0].Outcome.ShouldEqual(SemanticSpecificationOutcome.Passed);
    [Fact] void should_compare_the_response_not_only_the_events() => _results[1].Outcome.ShouldEqual(SemanticSpecificationOutcome.Failed);
    [Fact] void should_refuse_a_missing_generation_fixture() => _results[2].Unsupported!.Capability.ShouldEqual(StageExecutionCapability.IdentityAllocation);

    sealed class MissingResponseFixture() : Exception("The generated-value corpus must contain a response expectation.");
}
