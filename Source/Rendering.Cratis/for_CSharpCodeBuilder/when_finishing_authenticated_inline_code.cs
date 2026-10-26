// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CSharpCodeBuilder;

public class when_finishing_authenticated_inline_code : Specification
{
    [Fact]
    void should_finish_generated_skeleton_without_exposing_authored_body()
    {
        var seen = string.Empty;
        var builder = new CSharpCodeBuilder().OpenBlock("class Generated").OpenBlock("string Run()")
            .Raw("return \"Generated\";").EndBlock().EndBlock();
        var output = builder.ToString(skeleton =>
        {
            seen = skeleton;
            return skeleton.Replace("Generated", "Renamed", StringComparison.Ordinal);
        });
        Assert.DoesNotContain("return \"Generated\"", seen);
        Assert.Contains(InlineCodeTokens.Prefix, seen);
        Assert.Contains("class Renamed", output);
        Assert.Contains("return \"Generated\";", output);
        Assert.DoesNotContain(InlineCodeTokens.Prefix, output);
    }

    [Fact]
    void should_not_rewrite_authored_imports_or_generated_looking_strings()
    {
        const string body = "return \"global::System.String [Stage::member] Generated\";";
        var output = new CSharpCodeBuilder().OpenBlock("class Generated").OpenBlock("string Run()")
            .RawVerbatim(body).EndBlock().EndBlock().ToString(text => text.Replace("Generated", "Renamed", StringComparison.Ordinal));
        Assert.Contains(body, output);
        Assert.Contains("class Renamed", output);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    void should_refuse_finish_that_omits_or_duplicates_a_token(bool duplicate)
    {
        var builder = new CSharpCodeBuilder().Raw("return 1;");
        var exception = Assert.Throws<InlineCodeRejected>(() => builder.ToString(text => duplicate ? text + text : string.Empty));
        Assert.NotEmpty(exception.Diagnostics);
    }

    [Theory]
    [InlineData("return 1; /*")]
    [InlineData("} public class Evil {} {")]
    void should_refuse_unsafe_raw_bodies_at_actual_builder_boundary(string body)
    {
        var builder = new CSharpCodeBuilder().Raw(body);
        Assert.Throws<InlineCodeRejected>(builder.ToString);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    void should_refuse_moving_authenticated_body_to_another_scope_or_literal(bool literal)
    {
        var builder = new CSharpCodeBuilder().OpenBlock("class Host").OpenBlock("void First()")
            .Raw("return;").EndBlock().OpenBlock("void Second()").EndBlock().EndBlock();
        Assert.Throws<InlineCodeRejected>(() => builder.ToString(skeleton =>
        {
            var start = skeleton.IndexOf(InlineCodeTokens.Prefix, StringComparison.Ordinal);
            var end = skeleton.IndexOf(InlineCodeTokens.Suffix, start, StringComparison.Ordinal) + InlineCodeTokens.Suffix.Length;
            var token = skeleton[start..end];
            return literal ? skeleton.Replace(token, "\"" + token + "\"", StringComparison.Ordinal) :
                skeleton.Replace(token, string.Empty, StringComparison.Ordinal).Replace("void Second()", token + "void Second()", StringComparison.Ordinal);
        }));
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    void should_preserve_whole_authored_types_in_namespace_or_type_slots(bool nested, bool verbatim)
    {
        const string source = "public class Authored { public string Value => \"global::System.String [Stage::member]\"; }";
        var builder = new CSharpCodeBuilder().Namespace("Example");
        if (nested) builder.OpenBlock("public class Host");
        if (verbatim) builder.RawVerbatim(source);
        else builder.Raw(source);
        if (nested) builder.EndBlock();
        var output = builder.ToString();
        Assert.Contains((nested && !verbatim ? "    " : string.Empty) + source + "\n", output);
        Assert.DoesNotContain(InlineCodeTokens.Prefix, output);
    }

    [Theory]
    [InlineData("class Authored { string Value => \"\"\"unterminated")]
    [InlineData("class Authored { /*")]
    [InlineData("#if false\nclass Authored {}")]
    void should_refuse_unproven_declaration_slots_in_both_legacy_modes(string source)
    {
        var raw = new CSharpCodeBuilder().Namespace("Example").Raw(source);
        var verbatim = new CSharpCodeBuilder().Namespace("Example").RawVerbatim(source);
        Assert.Throws<InlineCodeRejected>(raw.ToString);
        Assert.Throws<InlineCodeRejected>(verbatim.ToString);
    }

    [Fact]
    void should_still_refuse_authored_type_declarations_inside_a_generated_member()
    {
        var builder = new CSharpCodeBuilder().OpenBlock("class Host").OpenBlock("void Run()")
            .Raw("public class Authored {}").EndBlock().EndBlock();
        Assert.Throws<InlineCodeRejected>(builder.ToString);
    }

    [Fact]
    void should_render_deterministically_and_not_execute_body()
    {
        static string Render() => new CSharpCodeBuilder().OpenBlock("class Host").OpenBlock("void Run()")
            .RawVerbatim("throw new global::System.Exception(\"authored body must not run\");").EndBlock().EndBlock().ToString();
        Render().ShouldEqual(Render());
    }
}
