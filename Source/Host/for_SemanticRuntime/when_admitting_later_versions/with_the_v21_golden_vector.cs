// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Screenplay.Semantics.Serialization;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticRuntime.when_admitting_later_versions;

public class with_the_v21_golden_vector : Specification
{
    SemanticExecutionPlan _plan = null!;
    SemanticRuntimeAdmission _admission = null!;

    void Establish() => _plan = SemanticExecutionPlan.Compile(SemanticModelSerializer.Deserialize(EsmGoldenVectors.Get(21).Bytes.AsSpan()), []).Plan!;
    void Because() => _admission = new(_plan);

    [Fact] void should_read_the_full_v21_vector() => _plan.Model.SemanticVersion.ShouldEqual(SemanticVersion.V21);
    [Fact] void should_not_refuse_the_audited_version() => _admission.Blocking.Any(entry => entry.Details!.StartsWith("STAGE-ESM-016:", StringComparison.Ordinal)).ShouldBeFalse();
    [Theory]
    [InlineData("rule")]
    void should_refuse_each_unimplemented_strategy(string strategy) => _admission.Blocking.Any(entry => entry.Details!.StartsWith("STAGE-ESM-028:", StringComparison.Ordinal) && entry.Details.Contains($"generate {strategy}", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_admit_checksum_validation() => _admission.Blocking.Any(entry => entry.Details!.StartsWith("STAGE-ESM-042:", StringComparison.Ordinal)).ShouldBeFalse();
}
