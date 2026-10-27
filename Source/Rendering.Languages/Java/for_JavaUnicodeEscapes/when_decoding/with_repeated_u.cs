// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Languages.Java.for_JavaUnicodeEscapes.when_decoding;

public class with_repeated_u : Specification
{
    string _result = string.Empty;

    void Because() => _result = JavaUnicodeEscapes.Decode(@"\uuuu0041\u00e9\uuD83D\uDE00");

    [Fact] void should_decode_repeated_u_and_mixed_case_hex() => _result.ShouldEqual("Aé😀");
}
