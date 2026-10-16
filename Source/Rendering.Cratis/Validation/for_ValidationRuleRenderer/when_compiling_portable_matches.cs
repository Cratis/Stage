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
    [InlineData("^[A-Z]{3}$", "ABC", true)]
    [InlineData("^[A-Z]{3}$", "ABC\n", false)]
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
        var code = $$"""
            public record Probe(string Value);
            public class ProbeValidator : AbstractValidator<Probe>
            {
                public ProbeValidator() => RuleFor(_ => _.Value){{call}};
            }
            """;
        var assembly = RenderedOutput.Load([new("Probe.cs", code)]);
        var probe = assembly.GetType("Probe")!;
        var validator = (IValidator)Activator.CreateInstance(assembly.GetType("ProbeValidator")!)!;
        var instance = Activator.CreateInstance(probe, input)!;
        var context = (IValidationContext)Activator.CreateInstance(typeof(ValidationContext<>).MakeGenericType(probe), instance)!;
        validator.Validate(context).IsValid.ShouldEqual(expected);
    }
}
