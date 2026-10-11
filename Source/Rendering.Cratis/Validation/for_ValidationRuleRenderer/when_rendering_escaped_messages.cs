// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.for_CratisRenderer;
using FluentValidation;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Validation.for_ValidationRuleRenderer;

public class when_rendering_escaped_messages : Specification
{
    [Theory]
    [InlineData("line\n\"quoted\"\\tail")]
    [InlineData("line\rreturn\ttab")]
    [InlineData("separator\u2028paragraph\u2029end")]
    [InlineData("non-BMP \U0001D11E")]
    public void should_compile_and_preserve_the_authored_message(string message)
    {
        var rule = new ValidationRuleSyntax("value", ValidationRuleKind.NotEmpty, null, message, SourceLocation.Start);
        var suffix = ValidationRuleRenderer.RenderMessage(rule);
        var code = $$"""
            public record MessageProbe(string Value);
            public class MessageProbeValidator : AbstractValidator<MessageProbe>
            {
                public MessageProbeValidator() => RuleFor(_ => _.Value).NotEmpty(){{suffix}};
            }
            """;
        var assembly = RenderedOutput.Load([new RenderedFile("MessageProbe.cs", code)]);
        var probe = assembly.GetType("MessageProbe")!;
        var validator = (IValidator)Activator.CreateInstance(assembly.GetType("MessageProbeValidator")!)!;
        var instance = Activator.CreateInstance(probe, string.Empty)!;
        var context = (IValidationContext)Activator.CreateInstance(typeof(ValidationContext<>).MakeGenericType(probe), instance)!;
        validator.Validate(context).Errors.Single().ErrorMessage.ShouldEqual(message);
    }
}
