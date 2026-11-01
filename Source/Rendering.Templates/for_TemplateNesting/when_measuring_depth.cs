// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Scriban.Syntax;
using Xunit;

namespace Cratis.Stage.Rendering.Templates.for_TemplateNesting;

public class when_measuring_depth : Specification
{
    [Fact]
    public void should_count_an_anonymous_function_once()
    {
        Catch.Exception(() => TemplateNesting.Check(Functions(32))).ShouldBeNull();
    }

    [Fact]
    public void should_refuse_33_anonymous_functions()
    {
        var refusal = Assert.Throws<TemplateRefusal>(() => TemplateNesting.Check(Functions(33)));
        refusal.Diagnostic.Code.ShouldEqual(TemplateCodes.Nesting);
    }

    [Fact]
    public void should_not_count_or_recursively_visit_block_wrappers()
    {
        ScriptStatement root = new ScriptBlockStatement();
        for (var index = 0; index < 512; index++)
        {
            var wrapper = new ScriptBlockStatement();
            wrapper.Statements.Add(root);
            root = wrapper;
        }
        Catch.Exception(() => TemplateNesting.Check(root)).ShouldBeNull();
    }

    static ScriptStatement Functions(int count)
    {
        ScriptStatement body = new ScriptBlockStatement();
        for (var index = 0; index < count; index++)
        {
            body = new ScriptExpressionStatement
            {
                Expression = new ScriptAnonymousFunction { Function = new ScriptFunction { Body = body } }
            };
        }
        return body;
    }
}
