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

    [Fact]
    void should_render_deterministically_and_not_execute_body()
    {
        static string Render() => new CSharpCodeBuilder().OpenBlock("class Host").OpenBlock("void Run()")
            .RawVerbatim("throw new global::System.Exception(\"authored body must not run\");").EndBlock().EndBlock().ToString();
        Render().ShouldEqual(Render());
    }
}
