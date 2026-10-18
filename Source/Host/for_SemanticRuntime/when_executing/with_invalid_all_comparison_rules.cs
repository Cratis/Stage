// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Security.Claims;
using System.Text.Json;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Host.for_SemanticRuntime.given;
using Cratis.Stage.Semantics;
using NSubstitute;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticRuntime.when_executing;

public class with_invalid_all_comparison_rules : Specification
{
    [Theory]
    [InlineData(SemanticValidationRuleKind.AllGreaterThan, true)]
    [InlineData(SemanticValidationRuleKind.AllGreaterThanOrEqual, true)]
    [InlineData(SemanticValidationRuleKind.AllGreaterThan, false)]
    [InlineData(SemanticValidationRuleKind.AllGreaterThanOrEqual, false)]
    public async Task should_refuse_the_rule_before_evaluation(SemanticValidationRuleKind kind, bool invalidOperand)
    {
        var original = compiled_plan.From("""
            module Comparisons
              feature Checking
                slice StateChange Check
                  command Check
                    values Int[]
                    answer Bool
                    validate
                      values all > 0
                    returns answer
            """);
        var model = original.Model;
        var module = model.Application.Modules.Single();
        var feature = module.Features.Single();
        var slice = feature.Slices.Single();
        var command = slice.Commands.Single();
        var rule = command.Validations.Single() with { Kind = kind };
        command = invalidOperand
            ? command with { Validations = [rule with { Operand = SemanticValue.Text("zero") }] }
            : command with { Validations = [rule], Properties = [command.Properties[0] with { Type = command.Properties[0].Type with { Primitive = SemanticPrimitiveType.Boolean } }, command.Properties[1]] };
        var application = model.Application with { Modules = [module with { Features = [feature with { Slices = [slice with { Commands = [command] }] }] }] };

        // Fault injection bypasses Screenplay's contract checks to exercise Stage's fail-closed boundary.
        typeof(ExecutableSemanticModel).GetField("<Application>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(model, application);
        var plan = SemanticExecutionPlan.Compile(model).Plan!;
        var details = invalidOperand
            ? $"Command 'Check' validation '{kind}' on property 'values' requires a numeric operand."
            : $"Command 'Check' validation '{kind}' on property 'values' requires a collection of numeric primitives or concepts.";
        var refusal = new SemanticRuntimeAdmission(plan).Blocking.Single();
        refusal.Artifact.ShouldEqual(command.Id.ToString());
        refusal.Details.ShouldEqual(details);
        var appender = Substitute.For<IAppendSemanticFacts>();
        var runtime = SemanticRuntimeHosting.Create(plan, appender);
        using var lifetime = (IDisposable)runtime;
        var result = await runtime.Execute(command, new Dictionary<string, JsonElement> { ["values"] = JsonSerializer.SerializeToElement(Array.Empty<int>()) }, new ClaimsPrincipal(), new(DateTimeOffset.UnixEpoch, "subject", "name", "user"), false);
        result.ShouldBeOfExactType<SemanticUnsupported>();
        var unsupported = (SemanticUnsupported)result;
        unsupported.Capability.ShouldEqual(SemanticExecutionCapability.Command);
        unsupported.Details.ShouldEqual(details);
        await appender.DidNotReceive().Append(Arg.Any<IReadOnlyList<SemanticFact>>(), Arg.Any<SemanticCommandOccurrence>());
    }
}
