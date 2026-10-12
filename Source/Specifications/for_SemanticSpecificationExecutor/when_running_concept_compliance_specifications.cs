// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Screenplay.Semantics.Serialization;
using Cratis.Stage.Contracts.Specifications.Semantic;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor;

public class when_running_concept_compliance_specifications : Specification
{
    SemanticExecutionPlan _plan = null!;
    SemanticSpecificationRunReport _report = null!;

    void Establish() => _plan = SemanticExecutionPlan.Compile(SemanticModelSerializer.Deserialize(ConceptComplianceCorpus.V13.EsmBytes.AsSpan())).Plan!;
    async Task Because() => _report = await new SemanticSpecificationExecutor().Run(_plan, new([]), new());

    [Fact] void should_admit_v13() => _plan.Model.SemanticVersion.ShouldEqual(SemanticVersion.V13);
    [Fact] void should_run_the_protected_information_case() => _report.Results.Count.ShouldEqual(1);
    [Fact] void should_preserve_the_outcome() => _report.Results.Single().Outcome.ShouldEqual(SemanticSpecificationOutcome.Passed);
}
