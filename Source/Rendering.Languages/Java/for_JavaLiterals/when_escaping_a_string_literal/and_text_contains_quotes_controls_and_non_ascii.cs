// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Languages.Java.for_JavaLiterals.when_escaping_a_string_literal;

public class and_text_contains_quotes_controls_and_non_ascii : Specification
{
    string _result = string.Empty;

    void Because() => _result = JavaLiterals.EscapeString("\"'\\\b\t\n\f\r\0é😀");

    [Fact] void should_escape_every_code_unit_deterministically() => _result.ShouldEqual("\"\\\"'\\\\\\b\\t\\n\\f\\r\\u0000\\u00e9\\ud83d\\ude00\"");
}
