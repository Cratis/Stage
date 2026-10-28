// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Languages.Java.for_JavaLiterals;

public class when_escaping_a_character_literal : Specification
{
    string[] _results = [];

    void Because() => _results = [.. new[] { '\'', '"', '\\', '\n', '\0', 'é', 'A' }.Select(JavaLiterals.EscapeCharacter)];

    [Fact] void should_quote_and_escape_character_values() => _results.ShouldEqual(["'\\''", "'\"'", "'\\\\'", "'\\n'", "'\\u0000'", "'\\u00e9'", "'A'"]);
}
