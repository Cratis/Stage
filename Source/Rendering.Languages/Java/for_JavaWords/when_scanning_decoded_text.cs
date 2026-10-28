// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Languages.Java.for_JavaWords;

public class when_scanning_decoded_text : Specification
{
    JavaWordSet _words = null!;

    void Because() => _words = JavaWords.Scan("\\u0053tring Str\u0000ing Con\u200dstant Sup\U000e0001plement /* CommentName */ \"LiteralName\" 12NotAWord $Dollar _under");

    [Fact] void should_decode_escaped_identifiers() => _words.All.Contains("String").ShouldBeTrue();
    [Fact] void should_join_names_separated_by_ignorable_controls() => _words.All.Contains("Constant").ShouldBeTrue();
    [Fact] void should_join_names_separated_by_supplementary_format_characters() => _words.All.Contains("Supplement").ShouldBeTrue();
    [Fact] void should_also_keep_the_separated_words() => _words.All.IsSupersetOf(["Str", "ing", "Con", "stant", "Sup", "plement"]).ShouldBeTrue();
    [Fact] void should_count_words_in_comments_and_strings() => _words.All.IsSupersetOf(["CommentName", "LiteralName"]).ShouldBeTrue();
    [Fact] void should_drop_the_whole_run_when_it_starts_with_a_digit() => _words.All.Contains("NotAWord").ShouldBeFalse();
    [Fact] void should_allow_dollar_and_underscore_in_word_runs() => _words.All.IsSupersetOf(["$Dollar", "_under"]).ShouldBeTrue();
}
