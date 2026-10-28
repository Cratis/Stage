// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Languages.Java.for_JavaLiterals;

public class when_escaping_comment_text : Specification
{
    string _result = string.Empty;

    void Because() => _result = JavaLiterals.EscapeLineComment("before\r\n\t\u2028\u2029\u0085" + @"\u000a \\\uu000d é after");

    [Fact] void should_flatten_controls_and_double_every_backslash() => _result.ShouldEqual("before      " + @"\\u000a \\\\\\uu000d é after");
    [Fact] void should_prevent_unicode_translation_of_comment_text() => JavaUnicodeEscapes.Decode(_result).ShouldEqual(_result);
}
