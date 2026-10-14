// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Screenplay.Semantics.Serialization;
using Cratis.Stage.Contracts.Specifications.Semantic;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor;

public class when_admitting_the_v17_golden_vector : Specification
{
    SemanticExecutionPlan _plan = null!;
    SemanticSpecificationRunReport _report = null!;

    void Establish() => _plan = SemanticExecutionPlan.Compile(SemanticModelSerializer.Deserialize(EsmGoldenVectors.Get(17).Bytes.AsSpan()), []).Plan!;
    async Task Because() => _report = await new SemanticSpecificationExecutor().Run(_plan, new([]), new());

    [Fact] void should_read_the_full_v17_vector() => _plan.Model.SemanticVersion.ShouldEqual(SemanticVersion.V17);
    [Fact] void should_run_a_nonempty_specification_set() => _report.Results.ShouldNotBeEmpty();
    [Fact] void should_not_refuse_the_audited_version() => _report.Results.Any(result => result.Unsupported?.Details.StartsWith("STAGE-ESM-016:", StringComparison.Ordinal) == true).ShouldBeFalse();
}
