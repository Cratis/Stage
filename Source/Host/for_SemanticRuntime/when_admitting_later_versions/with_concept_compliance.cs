// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Screenplay.Semantics.Serialization;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticRuntime.when_admitting_later_versions;

public class with_concept_compliance : Specification
{
    SemanticExecutionPlan _plan = null!;
    SemanticRuntimeAdmission _admission = null!;

    void Establish() => _plan = SemanticExecutionPlan.Compile(SemanticModelSerializer.Deserialize(ConceptComplianceCorpus.V13.EsmBytes.AsSpan())).Plan!;
    void Because() => _admission = new(_plan);

    [Fact] void should_admit_v13() => _plan.Model.SemanticVersion.ShouldEqual(SemanticVersion.V13);
    [Fact] void should_admit_every_construct() => _admission.Blocking.ShouldBeEmpty();
}
