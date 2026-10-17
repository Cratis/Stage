// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Screenplay.Semantics.Serialization;
using Cratis.Stage.Contracts.Specifications.Semantic;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor;

public class when_admitting_opaque_generated_concept_rules : Specification
{
    SemanticExecutionPlan _plan = null!;
    SemanticSpecificationRunReport _report = null!;
    SemanticId _concept;

    void Establish()
    {
        var model = SemanticModelSerializer.Deserialize(GeneratedConceptRulesCorpus.PortableV20.EsmBytes.AsSpan());
        _plan = SemanticExecutionPlan.Compile(model).Plan!;
        _concept = model.Application.Concepts.Single(concept => concept.Name == "ReviewedCode").Id;
    }

    async Task Because() => _report = await new SemanticSpecificationExecutor().Run(_plan, new([]), new());

    [Fact] void should_report_every_specification_without_throwing() => _report.Results.Count.ShouldEqual(_plan.Specifications.Count);
    [Fact] void should_accept_the_proven_customer_identifier() => _report.Results.Single(result => result.Name == "ProvenCustomerId").Outcome.ShouldEqual(SemanticSpecificationOutcome.Passed);
    [Fact] void should_preserve_the_accepted_customer_outcome() => _report.Results.Single(result => result.Name == "ProvenCustomerId").ExecutionKind.ShouldEqual("Accepted");
    [Fact] void should_reject_the_invalid_invite_fixture() => _report.Results.Single(result => result.Name == "InvalidInviteFixture").Outcome.ShouldEqual(SemanticSpecificationOutcome.Passed);
    [Fact] void should_preserve_the_rejected_invite_outcome() => _report.Results.Single(result => result.Name == "InvalidInviteFixture").ExecutionKind.ShouldEqual("Rejected");
    [Fact] void should_refuse_only_the_opaque_invite_rule() => _report.Results.Single(result => result.Name == "OpaqueInviteRule").Outcome.ShouldEqual(SemanticSpecificationOutcome.Unsupported);
    [Fact] void should_identify_the_opaque_concept_capability() => _report.Results.Single(result => result.Name == "OpaqueInviteRule").Unsupported!.Capability.ShouldEqual(StageExecutionCapability.Command);
    [Fact] void should_identify_the_opaque_concept() => _report.Results.Single(result => result.Name == "OpaqueInviteRule").Unsupported!.Construct.ShouldEqual(_concept.ToString());
    [Fact] void should_report_the_implementation_body_code() => _report.Results.Single(result => result.Name == "OpaqueInviteRule").Unsupported!.Details.ShouldContain("STAGE-IMPL-003: ");
}
