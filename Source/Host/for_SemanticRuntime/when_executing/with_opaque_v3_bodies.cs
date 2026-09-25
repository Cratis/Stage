// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using System.Text.Json;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Host.for_SemanticRuntime.given;
using Cratis.Stage.Semantics;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticRuntime.when_executing;

public class with_opaque_v3_bodies : a_semantic_runtime
{
    SemanticExecutionResult _code = null!;
    SemanticExecutionResult _predicate = null!;
    SemanticExecutionResult _concept = null!;
    SemanticExecutionResult _reducer = null!;
    SemanticExecutionResult _reducerQuery = null!;

    async Task Because()
    {
        _code = await Execute(slice => slice with { Commands = [slice.Commands.Single() with { CodeValidations = [new("code-body")] }] });
        _predicate = await Execute(slice => slice with { Commands = [slice.Commands.Single() with
        {
            Validations = [new(slice.Commands[0].Properties[1].Id, SemanticValidationRuleKind.RulePredicate, null, null)
            {
                Name = "CheckProject", RequirementId = "predicate-body"
            }]
        }] });
        var concept = _runtime.Plan.Model.Application.Concepts[0];
        _concept = await Execute(slice => slice, application => application with
        {
            Concepts = [.. application.Concepts.Select(value => value.Id == concept.Id ? value with
            {
                Validations = [new(default, SemanticValidationRuleKind.CodeValidation, null, null)
                {
                    Name = "CheckConcept", RequirementId = "concept-body"
                }]
            } : value)]
        });
        var readModel = _runtime.Plan.ReadModels.Values.First();
        SemanticSlice WithReducer(SemanticSlice slice) => slice with
        {
            Reducers = [new("ProjectReducer", readModel.Id, [new(slice.Events[0].Id, "reducer-body")])]
        };
        _reducer = await Execute(WithReducer);
        _reducerQuery = await Execute(WithReducer, query: true);
    }

    [Fact] void should_report_code_validation_as_unsupported() => (_code is SemanticUnsupported { Capability: SemanticExecutionCapability.Command }).ShouldBeTrue();
    [Fact] void should_report_rule_predicate_as_unsupported() => (_predicate is SemanticUnsupported { Capability: SemanticExecutionCapability.Command }).ShouldBeTrue();
    [Fact] void should_report_concept_code_validation_as_unsupported() => (_concept is SemanticUnsupported { Capability: SemanticExecutionCapability.Command }).ShouldBeTrue();
    [Fact] void should_report_reducer_as_unsupported() => (_reducer is SemanticUnsupported { Capability: SemanticExecutionCapability.Projection }).ShouldBeTrue();
    [Fact] void should_report_reducer_queries_as_unsupported() => (_reducerQuery is SemanticUnsupported { Capability: SemanticExecutionCapability.Projection }).ShouldBeTrue();

    async Task<SemanticExecutionResult> Execute(Func<SemanticSlice, SemanticSlice> change, Func<SemanticApplication, SemanticApplication>? changeApplication = null, bool query = false)
    {
        var application = _runtime.Plan.Model.Application;
        var module = application.Modules.Single();
        var feature = module.Features.Single();
        var original = feature.Slices.Single(slice => slice.Kind == SemanticSliceKind.StateChange);
        application = application with
        {
            Modules = [module with { Features = [feature with { Slices = [.. feature.Slices.Select(slice => slice.Id == original.Id ? change(slice) : slice)] }] }],
            Policies = [.. application.Policies, new SemanticPolicy("UnusedOpaquePolicy", new SemanticOpaquePolicyCondition("policy-body"))]
        };
        var model = ExecutableSemanticModel.Create(LanguageVersion.V3, SemanticVersion.V3, changeApplication is null ? application : changeApplication(application));
        var plan = SemanticExecutionPlan.Compile(model).Plan!;
        using var runtime = SemanticRuntimeHosting.Create(plan, (IAppendSemanticFacts)_appender) as IDisposable;
        return query
            ? await ((ISemanticRuntime)runtime!).Query(plan.Queries.Values.Single(), SemanticValue.Text("id"), new ClaimsPrincipal())
            : await ((ISemanticRuntime)runtime!).Execute(plan.Commands.Values.Single(), new Dictionary<string, JsonElement>(), new ClaimsPrincipal(), new(DateTimeOffset.UtcNow, "subject", "name", "user"), true);
    }
}
