// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Languages.Java.for_JavaUnicodeEscapes;

public class when_decoding_with_a_map : Specification
{
    (string Text, int[] RawIndexes) _result;

    void Because() => _result = JavaUnicodeEscapes.DecodeWithMap(@"a\u0042\\u0043c");

    [Fact] void should_decode_like_decode() => _result.Text.ShouldEqual(JavaUnicodeEscapes.Decode(@"a\u0042\\u0043c"));
    [Fact] void should_map_each_decoded_character_to_the_start_of_its_raw_form() => _result.RawIndexes.ShouldEqual([0, 1, 7, 8, 9, 10, 11, 12, 13, 14]);
}
