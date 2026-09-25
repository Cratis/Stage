// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Contracts.Specifications.Semantic;
using Cratis.Stage.Specifications.Admission;
using Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.given;
using Xunit;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.when_admitting_v3;

public class with_opaque_bodies : a_command_only_plan
{
    SemanticUnsupportedCapability? _code;
    SemanticUnsupportedCapability? _predicate;
    SemanticUnsupportedCapability? _concept;
    SemanticUnsupportedCapability? _reducer;

    void Because()
    {
        var command = _plan.Commands.Values.Single();
        _code = Check(slice => slice with { Commands = [command with { CodeValidations = [new("code-body")] }] });
        _predicate = Check(slice => slice with { Commands = [command with
        {
            Validations = [new(command.Properties[0].Id, SemanticValidationRuleKind.RulePredicate, null, null)
            {
                Name = "CheckProject", RequirementId = "predicate-body"
            }]
        }] });
        var concept = _plan.Model.Application.Concepts[0];
        _concept = Check(slice => slice, application => application with
        {
            Concepts = [.. application.Concepts.Select(value => value.Id == concept.Id ? value with
            {
                Validations = [new(default, SemanticValidationRuleKind.CodeValidation, null, null)
                {
                    Name = "CheckConcept", RequirementId = "concept-body"
                }]
            } : value)]
        });
        var readModel = _plan.ReadModels.Values.Single();
        _reducer = Check(slice => slice with
        {
            Reducers = [new("ProjectReducer", readModel.Id, [new(slice.Events[0].Id, "reducer-body")])]
        });
    }

    [Fact] void should_report_code_validation_as_unsupported_command() => _code!.Capability.ShouldEqual(StageExecutionCapability.Command);
    [Fact] void should_report_rule_predicate_as_unsupported_command() => _predicate!.Capability.ShouldEqual(StageExecutionCapability.Command);
    [Fact] void should_report_concept_code_validation_as_unsupported_command() => _concept!.Capability.ShouldEqual(StageExecutionCapability.Command);
    [Fact] void should_report_reducer_as_unsupported_projection() => _reducer!.Capability.ShouldEqual(StageExecutionCapability.Projection);

    SemanticUnsupportedCapability? Check(Func<SemanticSlice, SemanticSlice> change, Func<SemanticApplication, SemanticApplication>? changeApplication = null)
    {
        var application = _plan.Model.Application;
        var module = application.Modules.Single();
        var feature = module.Features.Single();
        var original = feature.Slices.Single(slice => slice.Kind == SemanticSliceKind.StateChange);
        application = application with
        {
            Modules = [module with { Features = [feature with { Slices = [.. feature.Slices.Select(slice => slice.Id == original.Id ? change(slice) : slice)] }] }],
            Policies = [.. application.Policies, new SemanticPolicy("UnusedOpaquePolicy", new SemanticOpaquePolicyCondition("policy-body"))]
        };
        var model = ExecutableSemanticModel.Create(LanguageVersion.V3, SemanticVersion.V3, changeApplication is null ? application : changeApplication(application));
        return SemanticRunAdmission.Check(SemanticExecutionPlan.Compile(model).Plan!, _specification);
    }
}
