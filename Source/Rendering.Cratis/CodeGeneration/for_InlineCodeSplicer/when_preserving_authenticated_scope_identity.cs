// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;
using static Cratis.Stage.Rendering.Cratis.CodeGeneration.for_InlineCode.when_verifying_original_bytes;

namespace Cratis.Stage.Rendering.Cratis.CodeGeneration.for_InlineCodeSplicer;

public class when_preserving_authenticated_scope_identity : Specification
{
    [Theory]
    [InlineData("class H { void A() { SLOT } void B() {} }", "class H { void B() { SLOT } }")]
    [InlineData("class A { void Run() { SLOT } } class B {}", "class B { void Run() { SLOT } }")]
    [InlineData("class H { void Run(int value) { SLOT } void Run(string value) {} }", "class H { void Run(string value) { SLOT } }")]
    [InlineData("class H { int A { get { SLOT } } int B { get { return 0; } } }", "class H { int B { get { SLOT } } }")]
    [InlineData("class H { void Run() { void A() { SLOT } void B() {} } }", "class H { void Run() { void B() { SLOT } } }")]
    [InlineData("class H { void Run() { if (first) {} else { SLOT } if (second) {} else {} } }", "class H { void Run() { if (second) {} else { SLOT } } }")]
    void should_refuse_relocation_plus_deletion_even_when_brace_ordinals_match(string original, string finished) => AssertRefused(original, finished);

    [Theory]
    [InlineData("class Original { void Run() { SLOT } }", "class Renamed { void Run() { SLOT } }")]
    [InlineData("class H { void Original() { SLOT } }", "class H { void Renamed() { SLOT } }")]
    [InlineData("namespace Original { class H { void Run() { SLOT } } }", "namespace Renamed { class H { void Run() { SLOT } } }")]
    [InlineData("namespace Original; class H { void Run() { SLOT } }", "namespace Renamed; class H { void Run() { SLOT } }")]
    [InlineData("namespace Original; SLOT class H {}", "namespace Renamed; SLOT class H {}")]
    void should_refuse_enclosing_type_member_or_namespace_renames(string original, string finished) => AssertRefused(original, finished);

    [Theory]
    [InlineData("class H { void Run() { int generated = 1; SLOT return; } }", "class H { void Run() { SLOT int generated = 1; return; } }")]
    [InlineData("class H { void Run() { SLOT } }", "class H { void Run(SLOT) {} }")]
    void should_refuse_changed_slot_boundaries_within_the_same_owner(string original, string finished) => AssertRefused(original, finished);

    [Fact]
    void should_allow_formatting_and_nonboundary_generated_expression_substitution()
    {
        var registry = new InlineCodeTokens();
        var token = registry.Issue(Code("return;"), Site).Token!;
        var original = $"class H {{ void Run() {{ var generated = \"[Stage::value]\"; {token} }} }}";
        var finished = $"// Finished skeleton\nclass H\n{{\n    void Run()\n    {{\n        var generated = \"Finished\";\n        {token}\n    }}\n}}";
        var result = InlineCodeSplicer.Splice(finished, registry, original);
        Assert.Empty(result.Diagnostics);
        Assert.Equal(finished.Replace(token.ToString(), "return;\n", StringComparison.Ordinal), result.Text);
        Assert.Single(result.Authored);
    }

    static void AssertRefused(string original, string finished)
    {
        var registry = new InlineCodeTokens();
        var token = registry.Issue(Code("return;"), Site).Token!.ToString();
        var result = InlineCodeSplicer.Splice(finished.Replace("SLOT", token, StringComparison.Ordinal), registry, original.Replace("SLOT", token, StringComparison.Ordinal));
        Assert.Null(result.Text);
        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("STAGE-INLINE-INTEGRITY", diagnostic.Code);
        Assert.Contains("scope or token order", diagnostic.Message);
    }
}
