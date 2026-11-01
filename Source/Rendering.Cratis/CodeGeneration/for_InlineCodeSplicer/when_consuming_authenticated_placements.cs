// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;
using static Cratis.Stage.Rendering.Cratis.CodeGeneration.for_InlineCode.when_verifying_original_bytes;

namespace Cratis.Stage.Rendering.Cratis.CodeGeneration.for_InlineCodeSplicer;

public class when_consuming_authenticated_placements : Specification
{
    [Fact]
    void should_splice_one_time_without_interpreting_body_tokens()
    {
        var registry = new InlineCodeTokens();
        var first = registry.Issue(Code("return 1;"), Site).Token;
        var body = $"// {first}\n// {InlineCodeTokens.Prefix}forged{InlineCodeTokens.Suffix}\nreturn 2;";
        var second = registry.Issue(Code(body), Site).Token;
        var result = InlineCodeSplicer.Splice($"before{first}middle{second}after", registry);
        result.Text.ShouldEqual("beforereturn 1;\nmiddle" + body + "\nafter");
        Assert.Equal(2, result.Authored.Length);
        Assert.Empty(result.Diagnostics);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    void should_refuse_missing_or_duplicate_token(int occurrences)
    {
        var registry = new InlineCodeTokens();
        var token = registry.Issue(Code("return 1;"), Site).Token!;
        var result = InlineCodeSplicer.Splice(string.Concat(Enumerable.Repeat(token.ToString(), occurrences)), registry);
        Assert.Null(result.Text);
        Assert.NotEmpty(result.Diagnostics);
    }

    [Fact]
    void should_refuse_foreign_render_forged_and_tampered_tokens_together()
    {
        var registry = new InlineCodeTokens();
        var token = registry.Issue(Code("return 1;"), Site).Token!;
        var other = new InlineCodeTokens().Issue(Code("return 1;"), Site).Token!;
        var tampered = token.ToString().Replace("stage_inline:", "stage_inline:0", StringComparison.Ordinal);
        var result = InlineCodeSplicer.Splice($"{other}{tampered}{InlineCodeTokens.Prefix}unkeyed-sha256{InlineCodeTokens.Suffix}", registry);
        Assert.Null(result.Text);
        Assert.Equal(4, result.Diagnostics.Length);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    void should_refuse_forged_or_tampered_token_even_when_the_genuine_token_is_present(bool tampered)
    {
        var registry = new InlineCodeTokens();
        var genuine = registry.Issue(Code("return 1;"), Site).Token!.ToString();
        var forged = tampered ? genuine.Replace("stage_inline:", "stage_inline:0", StringComparison.Ordinal) :
            InlineCodeTokens.Prefix + "forged" + InlineCodeTokens.Suffix;
        var result = InlineCodeSplicer.Splice(genuine + forged, registry);
        Assert.Null(result.Text);
        Assert.Single(result.Diagnostics);
        Assert.Contains("unauthenticated", result.Diagnostics.Single().Message);
    }

    [Fact]
    void should_refuse_token_in_a_sibling_member_without_a_missing_or_duplicate_token()
    {
        var registry = new InlineCodeTokens();
        var token = registry.Issue(Code("return;"), Site).Token;
        var original = $"class Host {{ void First() {{ {token} }} void Second() {{ }} }}";
        var moved = $"class Host {{ void First() {{ }} void Second() {{ {token} }} }}";
        var result = InlineCodeSplicer.Splice(moved, registry, original);
        Assert.Null(result.Text);
        Assert.Single(result.Diagnostics);
        Assert.Contains("scope or token order", result.Diagnostics.Single().Message);
    }

    [Fact]
    void should_refuse_reordering_genuine_tokens_within_the_same_member()
    {
        var registry = new InlineCodeTokens();
        var first = registry.Issue(Code("var first = 1;"), Site).Token;
        var second = registry.Issue(Code("var second = 2;"), Site).Token;
        var original = $"class Host {{ void Run() {{ {first}{second} }} }}";
        var reordered = $"class Host {{ void Run() {{ {second}{first} }} }}";
        var result = InlineCodeSplicer.Splice(reordered, registry, original);
        Assert.Null(result.Text);
        Assert.Single(result.Diagnostics);
        Assert.Contains("scope or token order", result.Diagnostics.Single().Message);
    }

    [Fact]
    void should_refuse_unterminated_token_and_wrong_role_owner_or_member()
    {
        Assert.Null(InlineCodeSplicer.Splice(InlineCodeTokens.Prefix, new()).Text);
        foreach (var placement in new[] { Site with { Role = "guard" }, Site with { Owner = "another" }, Site with { Member = "another" } })
        {
            var issued = new InlineCodeTokens().Issue(Code("return 1;"), placement);
            Assert.Null(issued.Token);
            Assert.NotEmpty(issued.Diagnostics);
        }
    }

    [Fact]
    void should_use_private_random_auth_without_changing_output_or_fingerprint()
    {
        var code = Code("return 1;");
        var first = new InlineCodeTokens();
        var second = new InlineCodeTokens();
        var a = first.Issue(code, Site).Token!;
        var b = second.Issue(code, Site).Token!;
        Assert.NotEqual(a.ToString(), b.ToString());
        InlineCodeSplicer.Splice(a.ToString(), first).Text.ShouldEqual(InlineCodeSplicer.Splice(b.ToString(), second).Text);
        CSharpInlineLanguagePack.Fingerprint(first.Slots.Values.Select(slot => slot.Code)).ShouldEqual(CSharpInlineLanguagePack.Fingerprint(second.Slots.Values.Select(slot => slot.Code)));
        Assert.Empty(typeof(InlineCodeToken).GetProperties());
    }

    [Fact]
    void should_change_provenance_for_each_semantic_input_but_not_input_order()
    {
        var code = Code("return 1;");
        var others = new[]
        {
            Code("return 2;"), Code("return 1;", "java"), Code("return 1;", placement: Site with { Role = "guard" }),
            Code("return 1;", placement: Site with { Owner = "other" }), Code("return 1;", placement: Site with { Member = "other" }),
            Code("return 1;", requirement: "other")
        };
        foreach (var other in others) Assert.NotEqual(CSharpInlineLanguagePack.Fingerprint([code]), CSharpInlineLanguagePack.Fingerprint([other]));
        CSharpInlineLanguagePack.Fingerprint(others).ShouldEqual(CSharpInlineLanguagePack.Fingerprint(others.Reverse()));
    }
}
