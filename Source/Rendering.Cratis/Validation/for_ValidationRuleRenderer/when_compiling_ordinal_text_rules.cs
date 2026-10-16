// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.for_CratisRenderer;
using Cratis.Stage.Rendering.Cratis.Semantics;
using FluentValidation;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Validation.for_ValidationRuleRenderer;

public class when_compiling_ordinal_text_rules : Specification
{
    [Theory]
    [InlineData(ValidationRuleKind.Contains, SemanticValidationRuleKind.Contains)]
    [InlineData(ValidationRuleKind.NotContains, SemanticValidationRuleKind.NotContains)]
    [InlineData(ValidationRuleKind.StartsWith, SemanticValidationRuleKind.StartsWith)]
    [InlineData(ValidationRuleKind.EndsWith, SemanticValidationRuleKind.EndsWith)]
    public void should_match_screenplay_in_both_renderers(ValidationRuleKind syntaxKind, SemanticValidationRuleKind semanticKind)
    {
        foreach (var operand in new[] { "ABC", "", ".*", "İ", "é" })
        {
            var rule = new SemanticValidationRule(default, semanticKind, SemanticValue.Text(operand), null);
            SemanticValidationRendering.CanRender(rule).ShouldBeTrue();
            var builder = new CSharpCodeBuilder();
            SemanticValidationRendering.Render(builder, rule, "value", SemanticPrimitiveType.Text, false, false);
            var call = ValidationRuleRenderer.RenderCall(syntaxKind, CSharpCodeBuilder.StringLiteral(operand));
            var code = $$"""
                public record Probe(string? Value);
                public class SyntaxValidator : AbstractValidator<Probe>
                {
                    public SyntaxValidator() => RuleFor(_ => _.Value){{call}};
                }
                public class SemanticValidator : AbstractValidator<Probe>
                {
                    public SemanticValidator()
                    {
                        {{builder}}
                    }
                }
                """;
            var assembly = RenderedOutput.Load([new("Probe.cs", code)]);
            var probe = assembly.GetType("Probe")!;
            foreach (var input in new[] { "ABC", "abc", "xABCx", "ABCx", "xABC", "", null, ".*", "AB\0C", "ABC\n", "İ", "i", "é", "e\u0301" })
            {
                var expected = SemanticValidationRules.Check(rule, input is null ? SemanticValue.Null : SemanticValue.Text(input)).Status == SemanticRuleStatus.Satisfied;
                var instance = Activator.CreateInstance(probe, [input])!;
                var context = (IValidationContext)Activator.CreateInstance(typeof(ValidationContext<>).MakeGenericType(probe), instance)!;
                foreach (var name in new[] { "SyntaxValidator", "SemanticValidator" })
                {
                    var validator = (IValidator)Activator.CreateInstance(assembly.GetType(name)!)!;
                    validator.Validate(context).IsValid.ShouldEqual(expected);
                }
            }
        }
    }
}
