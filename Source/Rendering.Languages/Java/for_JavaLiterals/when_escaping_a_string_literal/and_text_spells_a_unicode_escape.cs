// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Languages.Java.for_JavaLiterals.when_escaping_a_string_literal;

public class and_text_spells_a_unicode_escape : Specification
{
    string _result = string.Empty;

    void Because() => _result = JavaLiterals.EscapeString(@"\u000a");

    [Fact] void should_double_the_input_backslash() => _result.ShouldEqual("\"\\\\u000a\"");
    [Fact] void should_not_introduce_an_eligible_unicode_escape() => JavaUnicodeEscapes.Decode(_result).ShouldEqual(_result);
}
