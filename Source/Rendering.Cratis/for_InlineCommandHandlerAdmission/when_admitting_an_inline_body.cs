// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.Renderers;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_InlineCommandHandlerAdmission;

public class when_admitting_an_inline_body
{
    [Theory]
    [InlineData("return Array.Empty<object>();")]
    [InlineData("return new object[] { new EventNotAvailableDuringAdmission() };")]
    [InlineData("// context.Identity\nreturn new object[] { \"context\" };")]
    [InlineData("Func<object, object> identity = context => context; return new[] { identity(42) };")]
    [InlineData("object Identity(object context) => context; return new[] { Identity(42) };")]
    [InlineData("var value = new { context = 42 }; return new object[] { value.context };")]
    [InlineData("var value = new { context = 42 }; return new object[] { nameof(value.context) };")]
    [InlineData("return new object[] { \"#if DEBUG\" };")]
    public void should_accept_context_independent_bindings(string body) =>
        Catch.Exception(() => InlineCommandHandlerAdmission.EnsureAccepted(new("csharp", body, SourceLocation.Start), "Run", "Module.Feature.Run")).ShouldBeNull();

    [Theory]
    [InlineData("csharp", "return new object[] { context.Identity.Id };", InlineCommandHandlerRejectionReason.ContextBinding)]
    [InlineData("csharp", "var alias = context; return new object[] { alias };", InlineCommandHandlerRejectionReason.ContextBinding)]
    [InlineData("csharp", "Func<object> capture = () => context; return new[] { capture() };", InlineCommandHandlerRejectionReason.ContextBinding)]
    [InlineData("csharp", "object Capture() => context; return new[] { Capture() };", InlineCommandHandlerRejectionReason.ContextBinding)]
    [InlineData("csharp", "dynamic alias = context; return new object[] { alias.Identity };", InlineCommandHandlerRejectionReason.ContextBinding)]
    [InlineData("csharp", "return new object[] { nameof(context) };", InlineCommandHandlerRejectionReason.ContextBinding)]
    [InlineData("csharp", "return new object[] { @context };", InlineCommandHandlerRejectionReason.ContextBinding)]
    [InlineData("csharp", "return new object[] { con\\u0074ext };", InlineCommandHandlerRejectionReason.ContextBinding)]
    [InlineData("csharp", "return new object[] { context() };", InlineCommandHandlerRejectionReason.ContextBinding)]
    [InlineData("csharp", "return new object[] { Missing.context };", InlineCommandHandlerRejectionReason.UncertainContextBinding)]
    [InlineData("csharp", "dynamic value = Missing(); return new object[] { value.context };", InlineCommandHandlerRejectionReason.UncertainContextBinding)]
    [InlineData("csharp", "return new context<object>();", InlineCommandHandlerRejectionReason.ContextBinding)]
    [InlineData("csharp", "#if DEBUG\nreturn new object[] { context };\n#else\nreturn Array.Empty<object>();\n#endif", InlineCommandHandlerRejectionReason.Directives)]
    [InlineData("csharp", "#if false\ncontext.Identity.Id;\n#endif\nreturn Array.Empty<object>();", InlineCommandHandlerRejectionReason.Directives)]
    [InlineData("csharp", "#nullable disable\nreturn Array.Empty<object>();", InlineCommandHandlerRejectionReason.Directives)]
    [InlineData("csharp", "#pragma warning disable\nreturn Array.Empty<object>();", InlineCommandHandlerRejectionReason.Directives)]
    [InlineData("csharp", "return Array.Empty<object>();\n#region hidden\n#endregion", InlineCommandHandlerRejectionReason.Directives)]
    [InlineData("csharp", "return new object[;", InlineCommandHandlerRejectionReason.MalformedBody)]
    [InlineData("csharp", "return Array.Empty<object>(); } public void Extra() {", InlineCommandHandlerRejectionReason.MalformedBody)]
    [InlineData("csharp", "return Array.Empty<object>(); } garbage", InlineCommandHandlerRejectionReason.MalformedBody)]
    [InlineData("csharp", "return Array.Empty<object>(); /*", InlineCommandHandlerRejectionReason.MalformedBody)]
    [InlineData("javascript", "return [];", InlineCommandHandlerRejectionReason.UnsupportedLanguage)]
    [InlineData("unknown", "return Array.Empty<object>();", InlineCommandHandlerRejectionReason.UnsupportedLanguage)]
    public void should_fail_closed_with_the_exact_authored_diagnostic(string language, string body, InlineCommandHandlerRejectionReason reason)
    {
        var location = SourceLocation.Start with { Path = "authored/Handlers.play", Line = 31, Column = 13 };
        var code = new CodeBlockSyntax(language, body, location);
        var error = Catch.Exception(() => InlineCommandHandlerAdmission.EnsureAccepted(code, "ProcessBatch", "Billing.Invoices.ProcessBatch"));
        error.ShouldBeOfExactType<UnsupportedInlineCommandHandler>();
        var rejection = (UnsupportedInlineCommandHandler)error;
        rejection.CommandName.ShouldEqual("ProcessBatch");
        rejection.FullSlicePath.ShouldEqual("Billing.Invoices.ProcessBatch");
        rejection.Location.ShouldEqual(location);
        rejection.Reason.ShouldEqual(reason);
        rejection.Message.ShouldEqual(
            "STAGE-CRATIS-INLINE-001: Command 'ProcessBatch' in slice 'Billing.Invoices.ProcessBatch' at authored/Handlers.play(31,13) " +
            $"has an unsupported inline handler: {reason}. Only C# bodies proven independent of the generated context parameter can be rendered.");
    }
}
