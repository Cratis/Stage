// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.for_CratisRenderer;
using FluentValidation;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Validation.for_ValidationRuleRenderer;

public class when_compiling_portable_matches : Specification
{
    [Theory]
    [InlineData("b{2996}", "b", 2996, true)]
    [InlineData("b{2996}", "b", 2995, false)]
    [InlineData("^(ab){1498}$", "ab", 1498, true)]
    [InlineData("^(ab){1498}$", "ab", 1497, false)]
    [InlineData("^(a{2996}b)+$", "a", 2996, true)]
    [InlineData("^(a{2996}b)+$", "a", 2995, false)]
    [InlineData("(){999999999999999999999999999}", "one", 1, true)]
    public void should_apply_large_portable_patterns(string pattern, string unit, int count, bool expected)
    {
        var input = string.Concat(Enumerable.Repeat(unit, count));
        if (pattern == "^(a{2996}b)+$") input += "b";
        should_compile_and_apply_screenplay_matching(pattern, input, expected);
    }

    [Theory]
    [InlineData("^[A-Z]{3}$", "ABC", true)]
    [InlineData("^[A-Z]{3}$", "ABC\n", false)]
    [InlineData("^[A-Z]{3}$", "ABC\r", false)]
    [InlineData("^[A-Z]{3}$", "ABC\u2028", false)]
    [InlineData("^[^$]$", "A", true)]
    [InlineData(@"^\\$", "\\", true)]
    [InlineData(@"^\\$", "\\\n", false)]
    [InlineData(@"^[^\s@]+@[^\s@.]+(?:\.[^\s@.]+)+$", "a@b.co\n", false)]
    [InlineData(@"^\$$", "$", true)]
    [InlineData("^[$]$", "$", true)]
    [InlineData(@"^[\]$]+$", "]$", true)]
    [InlineData("^.$", "\r", false)]
    [InlineData("^.$", "\u2028", false)]
    [InlineData("^.$", "\u2029", false)]
    [InlineData(@"^\s$", "\u00a0", true)]
    [InlineData(@"^\s$", "\ufeff", true)]
    [InlineData(@"^\s$", "\u0085", false)]
    [InlineData(@"^\S$", "\u00a0", false)]
    [InlineData(@"^[\s]$", "\u2003", true)]
    [InlineData(@"^[^\s]$", "\u3000", false)]
    [InlineData(@"^\w$", "\u0130", false)]
    [InlineData(@"^\W$", "\u0130", true)]
    [InlineData(@"^[\w]$", "\u0130", false)]
    [InlineData(@"^\w$", "é", false)]
    [InlineData(@"^\d$", "\u0661", false)]
    [InlineData("^abc$", "ABC", false)]
    [InlineData("^.*$", null, true)]
    [InlineData("B", "ABC", true)]
    public void should_compile_and_apply_screenplay_matching(string pattern, string? input, bool expected)
    {
        var rule = new SemanticValidationRule(default, SemanticValidationRuleKind.Matches, SemanticValue.Text(pattern), null);
        (SemanticValidationRules.Check(rule, input is null ? SemanticValue.Null : SemanticValue.Text(input)).Status == SemanticRuleStatus.Satisfied).ShouldEqual(expected);
        var call = ValidationRuleRenderer.RenderCall(ValidationRuleKind.Matches, CSharpCodeBuilder.StringLiteral(pattern));
        var fields = new CSharpCodeBuilder();
        PortableMatchRendering.RenderFields(fields, [pattern]);
        var code = $$"""
            public record Probe(string Value);
            public class ProbeValidator : AbstractValidator<Probe>
            {
                {{fields}}
                public ProbeValidator() => RuleFor(_ => _.Value){{call}};
            }
            """;
        var assembly = RenderedOutput.Load([new("Probe.cs", code)]);
        var probe = assembly.GetType("Probe")!;
        var validator = (IValidator)Activator.CreateInstance(assembly.GetType("ProbeValidator")!)!;
        var cache = validator.GetType().GetField("_matchPatterns", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!;
        cache.IsInitOnly.ShouldBeTrue();
        var instance = Activator.CreateInstance(probe, input)!;
        var context = (IValidationContext)Activator.CreateInstance(typeof(ValidationContext<>).MakeGenericType(probe), instance)!;
        validator.Validate(context).IsValid.ShouldEqual(expected);
    }
}
