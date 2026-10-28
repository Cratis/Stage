// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Xunit;
using static Cratis.Stage.Rendering.Cratis.CodeGeneration.for_InlineCode.when_verifying_original_bytes;

namespace Cratis.Stage.Rendering.Cratis.CodeGeneration.for_CSharpInlineLanguagePack;

public class when_presenting_literal_sensitive_bodies : Specification
{
    [Theory]
    [InlineData("return 1;\r\n", "        return 1;\r\n")]
    [InlineData("return 1;", "        return 1;\n")]
    [InlineData("\nreturn 1;\n", "\n        return 1;\n")]
    void should_indent_only_presented_nonliteral_lines(string input, string expected) => CSharpInlineLanguagePack.Present(Code(input), 2).Text.ShouldEqual(expected);

    [Theory]
    [InlineData("return @\"line one\nline two\";")]
    [InlineData("return $@\"line {1}\nline two\";")]
    [InlineData("return @\"line one\r\nline two\";")]
    [InlineData("return \"\"\"\n    line one\n    line two\n    \"\"\";")]
    [InlineData("return $$\"\"\"\n    line {{1}}\n    line two\n    \"\"\";")]
    [InlineData("return \"\"\"\"\n    three quotes: \"\"\"\n    \"\"\"\";")]
    [InlineData("return @\"line one\u0085line two\";")]
    [InlineData("return @\"line one\u2028line two\";")]
    [InlineData("return @\"line one\u2029line two\";")]
    [InlineData("return $@\"line {1}\u0085line two\";")]
    [InlineData("return $@\"line {1}\u2028line two\";")]
    [InlineData("return $@\"line {1}\u2029line two\";")]
    [InlineData("return \"\"\"\u0085    line one\u0085    line two\u0085    \"\"\";")]
    [InlineData("return \"\"\"\u2028    line one\u2028    line two\u2028    \"\"\";")]
    [InlineData("return \"\"\"\u2029    line one\u2029    line two\u2029    \"\"\";")]
    void should_preserve_literal_tokens_values_and_compile_without_execution(string body)
    {
        var presentation = CSharpInlineLanguagePack.Present(Code(body), 3);
        Assert.NotNull(presentation.Text);
        var original = Parse(body);
        var presented = Parse(presentation.Text);
        Assert.Equal(
            original.GetRoot().DescendantTokens().Select(token => (token.RawKind, token.Text, token.ValueText)),
            presented.GetRoot().DescendantTokens().Select(token => (token.RawKind, token.Text, token.ValueText)));
        var compilation = CSharpCompilation.Create("LiteralProof", [presented], [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)], new(OutputKind.DynamicallyLinkedLibrary));
        Assert.Empty(compilation.GetDiagnostics().Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error));
        Assert.NotEmpty(presented.GetRoot().DescendantNodes().OfType<ReturnStatementSyntax>());
    }

    [Fact]
    void should_preserve_verbatim_bytes_and_refuse_unproved_language()
    {
        var code = Code("// keep\r\nreturn 1;\r\n");
        CSharpInlineLanguagePack.Present(code, 8, verbatim: true).Text.ShouldEqual(code.Text);
        Assert.Null(CSharpInlineLanguagePack.Present(Code("return 1;", "java")).Text);
    }

    [Theory]
    [InlineData("command")]
    [InlineData("operation")]
    [InlineData("generation")]
    void should_only_degrade_contained_unsupported_body_to_throwing_placeholder(string role)
    {
        var code = Code("#if true\nreturn 1;\n#endif", placement: Site with { Role = role });
        var result = CSharpInlineLanguagePack.Present(code);
        result.Degraded.ShouldBeTrue();
        Assert.Contains("throw new", result.Text);
        Assert.DoesNotContain("#if", result.Text);
        Assert.Single(result.Diagnostics);
        result.Diagnostics.Single().Code.ShouldEqual("STAGE-DEGRADE-003");
    }

    [Theory]
    [InlineData("return @\"unterminated")]
    [InlineData("} public class Escaped {} {")]
    [InlineData("return true; /*")]
    void should_refuse_foreign_invalid_body_without_a_placeholder(string source)
    {
        var result = CSharpInlineLanguagePack.Present(Code(source, "java"));
        Assert.Null(result.Text);
        result.Degraded.ShouldBeFalse();
        Assert.All(result.Diagnostics, diagnostic => Assert.Equal("STAGE-INLINE-INTEGRITY", diagnostic.Code));
    }

    [Fact]
    void should_only_degrade_foreign_language_when_language_independent_containment_is_provable()
    {
        var result = CSharpInlineLanguagePack.Present(Code(" \n", "java"));
        result.Degraded.ShouldBeTrue();
        Assert.NotNull(result.Text);
    }

    [Theory]
    [InlineData("guard")]
    [InlineData("validation")]
    void should_refuse_body_only_guard_substitution(string role)
    {
        var result = CSharpInlineLanguagePack.Present(Code("#if true\nreturn true;\n#endif", placement: Site with { Role = role }));
        result.Degraded.ShouldBeFalse();
        Assert.Null(result.Text);
        Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic.Code == "STAGE-DEGRADE-003");
    }

    static SyntaxTree Parse(string body) => CSharpSyntaxTree.ParseText("class Host { string Run() {\n" + body + "\n} }");
}
