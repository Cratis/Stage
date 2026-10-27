// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Templates.for_TemplatePreparer;

public class when_scanning_expression_blocks : given.a_kit
{
    [Fact]
    public void should_refuse_nested_conditions_interleaved_with_anonymous_functions()
    {
        var text = "{{ " + string.Concat(Enumerable.Repeat("if true; f = do; end;", 33)) + string.Concat(Enumerable.Repeat("end;", 33)) + " }}";
        var result = TemplatePreparer.Prepare(Kit(text));
        result.Diagnostics.Length.ShouldEqual(1);
        result.Diagnostics[0].Code.ShouldEqual(TemplateCodes.Nesting);
    }

    [Theory]
    [InlineData("if")]
    [InlineData("for")]
    [InlineData("while")]
    [InlineData("do")]
    [InlineData("func")]
    [InlineData("capture")]
    [InlineData("case")]
    [InlineData("with")]
    [InlineData("wrap")]
    [InlineData("tablerow")]
    public void should_conservatively_count_possible_openers_in_expressions(string keyword)
    {
        var text = "{{ x = (" + string.Concat(Enumerable.Repeat(keyword + " ", 33)) + ") }}";
        var result = TemplatePreparer.Prepare(Kit(text));
        result.Diagnostics.Length.ShouldEqual(1);
        result.Diagnostics[0].Code.ShouldEqual(TemplateCodes.Nesting);
    }

    [Fact]
    public void should_not_let_expression_identifiers_close_conditions()
    {
        var text = "{{ " + string.Concat(Enumerable.Repeat("if true; x =\nend;", 33)) + string.Concat(Enumerable.Repeat("end;", 33)) + " }}";
        var result = TemplatePreparer.Prepare(Kit(text));
        result.Diagnostics.Length.ShouldEqual(1);
        result.Diagnostics[0].Code.ShouldEqual(TemplateCodes.Nesting);
    }

    [Fact]
    public void should_refuse_nested_conditions_inside_a_parenthesized_anonymous_function()
    {
        var text = "{{ x = (do\n" + string.Concat(Enumerable.Repeat("if true\n", 33)) + string.Concat(Enumerable.Repeat("end\n", 33)) + "end) }}";
        var result = TemplatePreparer.Prepare(Kit(text));
        result.Diagnostics.Length.ShouldEqual(1);
        result.Diagnostics[0].Code.ShouldEqual(TemplateCodes.Nesting);
    }
}
