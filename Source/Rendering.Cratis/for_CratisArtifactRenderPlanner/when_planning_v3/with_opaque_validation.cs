// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_planning_v3.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_planning_v3;

public class with_opaque_validation : Specification
{
    ArtifactRenderPlan _code = null!;
    ArtifactRenderPlan _predicate = null!;
    ArtifactRenderPlan _concept = null!;

    void Because()
    {
        _code = a_v3_invoice.Plan(a_v3_invoice.Model(slice => slice with
        {
            Commands = [slice.Commands.Single() with { CodeValidations = [new("code-body")] }]
        }));
        _predicate = a_v3_invoice.Plan(a_v3_invoice.Model(slice => slice with
        {
            Commands = [slice.Commands.Single() with
            {
                Validations = [new(slice.Commands.Single().Properties[0].Id, SemanticValidationRuleKind.RulePredicate, null, null)
                {
                    Name = "CheckDescription", RequirementId = "predicate-body"
                }]
            }]
        }));
        var source = "concept OpaqueConcept : String\n" + invoice_model.Source("String", invoice_model.TextSource, invoice_model.OtherTextSource);
        var concept = invoice_model.Compile(source).Application.Concepts.Single();
        _concept = a_v3_invoice.Plan(a_v3_invoice.Model(applicationChange: application => application with
        {
            Concepts = [concept with { Validations = [new(default, SemanticValidationRuleKind.CodeValidation, null, null)
            {
                Name = "OpaqueConcept code validation", RequirementId = "concept-body"
            }] }]
        }));
    }

    [Fact] void should_reject_code_validation_without_artifacts() => Rejected(_code).ShouldBeTrue();
    [Fact] void should_reject_rule_predicate_without_artifacts() => Rejected(_predicate).ShouldBeTrue();
    [Fact] void should_reject_concept_code_validation_without_artifacts() => Rejected(_concept).ShouldBeTrue();

    static bool Rejected(ArtifactRenderPlan plan) => !plan.Success && plan.Artifacts.IsEmpty &&
        plan.Diagnostics.Any(diagnostic => diagnostic.Code == "STAGE-ESM-005" && diagnostic.Message.Contains("implementation bodies", StringComparison.Ordinal));
}
