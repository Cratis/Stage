// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Languages.Java.for_JavaLiterals.when_escaping_block_comment_text;

public class and_it_closes_a_block_comment : Specification
{
    string _result = string.Empty;

    void Because() => _result = JavaLiterals.EscapeComment("end */ and **/ then \\u002a/ \n done");

    [Fact] void should_break_every_comment_terminator() => _result.Contains("*/", StringComparison.Ordinal).ShouldBeFalse();
    [Fact] void should_separate_the_star_and_the_slash() => _result.StartsWith("end * / and ** / then ", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_still_double_backslashes() => _result.Contains("\\\\u002a", StringComparison.Ordinal).ShouldBeTrue();
}
