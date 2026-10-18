// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Contracts.Specifications.Semantic;
using Cratis.Stage.Semantics;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor;

public class when_refusing_invalid_all_comparison_rules : Specification
{
    [Theory]
    [InlineData(SemanticValidationRuleKind.AllGreaterThan, true)]
    [InlineData(SemanticValidationRuleKind.AllGreaterThanOrEqual, true)]
    [InlineData(SemanticValidationRuleKind.AllGreaterThan, false)]
    [InlineData(SemanticValidationRuleKind.AllGreaterThanOrEqual, false)]
    public async Task should_identify_the_unsupported_rule_shape(SemanticValidationRuleKind kind, bool invalidOperand)
    {
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Comparisons"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("model"), "model", "Model.play", """
            module Comparisons
              feature Checking
                slice StateChange Check
                  command Check
                    values Int[]
                    answer Bool
                    validate
                      values all > 0
                    returns answer
                  specification Empty
                    when Check
                      values = []
                      answer = true
                    then returns true
            """);
        var compilation = new SemanticModelCompiler().Compile("Comparisons", SemanticDocumentSet.Create([document], catalog));
        Assert.True(compilation.Success, string.Join(Environment.NewLine, compilation.Diagnostics));
        var model = compilation.Value!.Model;
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
        var report = await new SemanticSpecificationExecutor().Run(plan, new([]), new());
        var result = report.Results.Single();
        result.Outcome.ShouldEqual(SemanticSpecificationOutcome.Unsupported);
        result.Unsupported!.Capability.ShouldEqual(StageExecutionCapability.Command);
        result.Unsupported.Construct.ShouldEqual(command.Id.ToString());
        result.Unsupported.Details.ShouldEqual(invalidOperand
            ? $"Command 'Check' validation '{kind}' on property 'values' requires a numeric operand."
            : $"Command 'Check' validation '{kind}' on property 'values' requires a collection of numeric primitives or concepts.");
    }
}
