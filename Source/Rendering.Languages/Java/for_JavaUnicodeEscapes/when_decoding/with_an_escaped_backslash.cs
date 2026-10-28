// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Languages.Java.for_JavaUnicodeEscapes.when_decoding;

public class with_an_escaped_backslash : Specification
{
    string[] _results = [];

    void Because() => _results = [.. new[] { @"\u005cu0041", @"\u005c\u0041", @"\u005c\\u0041", @"\u005c\u005c\u0041", "raw\r\n\\" }.Select(JavaUnicodeEscapes.Decode)];

    [Fact] void should_not_recursively_decode_a_produced_backslash() => _results[0].ShouldEqual(@"\u0041");
    [Fact] void should_allow_a_raw_backslash_after_an_escape() => _results[1].ShouldEqual(@"\A");
    [Fact] void should_match_javac_unicode_reader_parity_after_a_produced_backslash() => _results[2].ShouldEqual(@"\\A");
    [Fact] void should_allow_successive_escaped_backslashes() => _results[3].ShouldEqual(@"\\A");
    [Fact] void should_preserve_raw_newlines_and_a_trailing_backslash() => _results[4].ShouldEqual("raw\r\n\\");
}
