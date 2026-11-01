// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Screenplay.Semantics.Serialization;
using Cratis.Specifications;
using Cratis.Stage.Semantics;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticRuntime.when_admitting_later_versions;

public class with_the_v13_golden_vector : Specification
{
    SemanticExecutionPlan _plan = null!;
    SemanticRuntimeAdmission _admission = null!;

    void Establish()
    {
        _plan = SemanticExecutionPlan.Compile(SemanticModelSerializer.Deserialize(EsmGoldenVectors.Get(13).Bytes.AsSpan()), []).Plan!;
    }

    void Because() => _admission = new(_plan);

    [Fact] void should_read_the_full_v13_vector() => _plan.Model.SemanticVersion.ShouldEqual(SemanticVersion.V13);
    [Fact] void should_preserve_existing_construct_refusals() => _admission.Blocking.ShouldNotBeEmpty();
    [Fact] void should_not_refuse_the_audited_version() => _admission.Blocking.Any(entry => entry.Details!.StartsWith("STAGE-ESM-016:", StringComparison.Ordinal)).ShouldBeFalse();
    [Fact] void should_not_misclassify_valid_compliance() => _admission.Blocking.Any(entry => entry.Details!.StartsWith("STAGE-ESM-040:", StringComparison.Ordinal)).ShouldBeFalse();
}
