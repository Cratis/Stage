// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Scriban;
using Xunit;

namespace Cratis.Stage.Rendering.Templates.for_TemplateNesting;

public class when_measuring_delimiter_depth : Specification
{
    [Fact]
    public void should_refuse_an_indexer_around_32_parentheses()
    {
        var refusal = Assert.Throws<TemplateRefusal>(() => Check("{{ model[" + Parenthesized(32) + "] }}"));
        refusal.Diagnostic.Code.ShouldEqual(TemplateCodes.Nesting);
    }

    [Fact]
    public void should_accept_an_indexer_around_31_parentheses()
    {
        Catch.Exception(() => Check("{{ model[" + Parenthesized(31) + "] }}")).ShouldBeNull();
    }

    [Fact]
    public void should_refuse_an_explicit_call_around_32_parentheses()
    {
        var refusal = Assert.Throws<TemplateRefusal>(() => Check("{{ f(" + Parenthesized(32) + ") }}"));
        refusal.Diagnostic.Code.ShouldEqual(TemplateCodes.Nesting);
    }

    [Fact]
    public void should_accept_an_explicit_call_around_31_parentheses()
    {
        Catch.Exception(() => Check("{{ f(" + Parenthesized(31) + ") }}")).ShouldBeNull();
    }

    [Fact]
    public void should_not_count_chained_indexer_targets()
    {
        Catch.Exception(() => Check("{{ model" + string.Concat(Enumerable.Repeat("[0]", 40)) + " }}")).ShouldBeNull();
    }

    static string Parenthesized(int depth) => new string('(', depth) + "0" + new string(')', depth);

    static void Check(string text)
    {
        var template = Template.Parse(text);
        template.HasErrors.ShouldBeFalse();
        TemplateNesting.Check(template.Page);
    }
}
