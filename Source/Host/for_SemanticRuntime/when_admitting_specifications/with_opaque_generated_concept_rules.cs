// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Screenplay.Semantics.Serialization;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Specifications.Semantic;
using Cratis.Stage.Semantics;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticRuntime.when_admitting_specifications;

public class with_opaque_generated_concept_rules : Specification
{
    SemanticExecutionPlan _plan = null!;
    SemanticRuntimeAdmission _admission = null!;

    void Establish() => _plan = SemanticExecutionPlan.Compile(SemanticModelSerializer.Deserialize(GeneratedConceptRulesCorpus.PortableV20.EsmBytes.AsSpan())).Plan!;

    void Because() => _admission = new(_plan);

    [Fact] void should_report_every_specification_without_throwing() => _admission.Entries.Count(entry => entry.Kind == "specification").ShouldEqual(_plan.Specifications.Count);
    [Fact] void should_refuse_only_the_opaque_specification() => _admission.Entries.Where(entry => entry.Kind == "specification" && entry.Status == "unsupported").Select(entry => entry.Artifact).ShouldContainOnly(_plan.Specifications.Values.Single(specification => specification.Name == "OpaqueInviteRule").Id.ToString());
    [Fact] void should_preserve_the_shared_executor_refusal() => _admission.Entries.Where(entry => entry.Kind == "specification").ShouldEachConformTo(entry => entry.Details == SemanticRunAdmission.Check(_plan, _plan.Specifications.Values.Single(specification => specification.Id.ToString() == entry.Artifact))?.Details);
    [Fact] void should_report_the_shared_typed_capability() => _admission.Entries.Single(entry => entry.Kind == "specification" && entry.Status == "unsupported").Capability.ShouldEqual(nameof(StageExecutionCapability.Command));
    [Fact] void should_report_the_implementation_body_code() => _admission.Entries.Single(entry => entry.Kind == "specification" && entry.Status == "unsupported").Details.ShouldContain("STAGE-IMPL-003: ");
}
