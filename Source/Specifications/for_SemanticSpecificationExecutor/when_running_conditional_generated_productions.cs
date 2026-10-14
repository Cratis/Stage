// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Screenplay.Semantics.Serialization;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Specifications.Semantic;
using Xunit;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor;

public class when_running_conditional_generated_productions : Specification
{
    SemanticSpecificationRunReport _report = null!;

    async Task Because()
    {
        var plan = SemanticExecutionPlan.Compile(SemanticModelSerializer.Deserialize(GeneratedValuesCorpus.V15.EsmBytes.AsSpan())).Plan!;
        _report = await new SemanticSpecificationExecutor().Run(plan, new([]), new());
    }

    [Fact] void should_pass_every_canonical_scenario() => _report.Results.Select(result => result.Outcome).ShouldContainOnly(SemanticSpecificationOutcome.Passed, SemanticSpecificationOutcome.Passed, SemanticSpecificationOutcome.Passed);
}
