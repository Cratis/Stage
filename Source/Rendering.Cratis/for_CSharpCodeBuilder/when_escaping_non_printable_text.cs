// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CSharpCodeBuilder;

public class when_escaping_non_printable_text : Specification
{
    string _literal = null!;

    void Because() => _literal = CSharpCodeBuilder.StringLiteral("First\u2028Second\u2029Third\u200BFourth");

    [Fact] void should_emit_unicode_escapes_for_line_and_format_separators() => _literal.ShouldEqual("\"First\\u2028Second\\u2029Third\\u200BFourth\"");
}
