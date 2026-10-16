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
    [Fact] void should_refuse_every_run_instead_of_evaluating_the_opaque_rule() => _report.Results.ShouldEachConformTo(result => result.Outcome == SemanticSpecificationOutcome.Unsupported);
    [Fact] void should_identify_the_opaque_concept_capability() => _report.Results.ShouldEachConformTo(result => result.Unsupported!.Capability == StageExecutionCapability.Command && result.Unsupported.Construct == _concept.ToString());
}
