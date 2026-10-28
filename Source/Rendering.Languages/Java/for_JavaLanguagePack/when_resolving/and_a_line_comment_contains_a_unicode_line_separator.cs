// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Declarative;
using Cratis.Stage.Rendering.Templates;
using Xunit;

namespace Cratis.Stage.Rendering.Languages.Java.for_JavaLanguagePack.when_resolving;

public class and_a_line_comment_contains_a_unicode_line_separator : given.a_java_pack
{
    ImmutableArray<DeclarativeFileResolution> _result;

    ImmutableArray<DeclarativeFileResolution> _sameLine;
    ImmutableArray<DeclarativeFileResolution> _after;

    void Because()
    {
        _result = Resolve(Class("example", "A", Txt("// note\u2028class Evil\n"), Reference("Evil", "lib"), Txt(" e;\n")));
        _sameLine = Resolve(Class("example", "B", Txt("// note\u2028"), Reference("Evil", "lib"), Txt(" e;\n")));
        _after = Resolve(Class("example", "C", Reference("Evil", "lib"), Txt(" e; // note\u2028class Evil\n")));
    }

    [Fact] void should_refuse_a_spelling_on_the_comment_line() => Code(_result[0]).ShouldEqual(JavaRefusalCodes.SpellingContext);
    [Fact] void should_refuse_a_spelling_that_follows_the_separator() => Code(_sameLine[0]).ShouldEqual(JavaRefusalCodes.SpellingContext);
    [Fact] void should_count_the_word_after_the_separator_as_a_word() => Text(_after[0]).ShouldEqual(Expected("C", "lib.Evil e; // note\u2028class Evil\n"));
}
