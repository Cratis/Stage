// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Scriban.Parsing;
using Xunit;

namespace Cratis.Stage.Rendering.Templates.for_TemplateNesting;

public class when_mapping_parser_limits : Specification
{
    [Theory]
    [InlineData("The statement depth limit `64` was reached when parsing this statement", TemplateCodes.Nesting)]
    [InlineData("The parser recursive depth limit was reached near a stack overflow", TemplateCodes.Nesting)]
    [InlineData("Unexpected expression token", TemplateCodes.Parse)]
    [InlineData("Unexpected token: The statement depth limit `64`", TemplateCodes.Parse)]
    public void should_preserve_parser_messages_and_locations_with_the_correct_code(string message, string code)
    {
        var position = new TextPosition(3, 1, 2);
        var error = new LogMessage(ParserMessageType.Error, new SourceSpan("main.scriban", position, position), message);
        var diagnostic = TemplateNesting.ParserFailure(error).Diagnostic;
        diagnostic.Code.ShouldEqual(code);
        diagnostic.Message.ShouldEqual(message);
        diagnostic.Path.ShouldEqual("main.scriban");
        diagnostic.Line.ShouldEqual(2);
        diagnostic.Column.ShouldEqual(3);
    }
}
