// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
    [InlineData("^[]$]+$", "]$", true)]
    [InlineData("^A[^]$", "AB\n", false)]
    [InlineData("B", "ABC", true)]
    public void should_compile_and_apply_screenplay_matching(string pattern, string input, bool expected)
    {
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
