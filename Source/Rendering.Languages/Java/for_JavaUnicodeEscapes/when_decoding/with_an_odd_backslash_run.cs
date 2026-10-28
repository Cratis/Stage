// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Languages.Java.for_JavaUnicodeEscapes.when_decoding;

public class with_an_odd_backslash_run : Specification
{
    string[] _results = [];

    void Because() => _results = [.. new[] { @"\u0041", @"\\\u0041", @"\\\\\u0041" }.Select(JavaUnicodeEscapes.Decode)];

    [Fact] void should_translate_the_last_backslash_in_an_odd_run() => _results.ShouldEqual(["A", @"\\A", @"\\\\A"]);
}
