// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Declarative;
using Cratis.Stage.Rendering.Templates;
using Xunit;

namespace Cratis.Stage.Rendering.Languages.Java.for_JavaLanguagePack.when_resolving;

public class and_a_spelling_touches_an_identifier_char : given.a_java_pack
{
    ImmutableArray<DeclarativeFileResolution> _result;

    void Because() => _result = Resolve(
        Class("example", "A1", Txt("a"), Reference("Thing", "lib"), Txt(" f;\n")),
        Class("example", "A2", Txt("\u00e9"), Reference("Thing", "lib"), Txt(" f;\n")),
        Class("example", "A3", Txt("\\"), Reference("Thing", "lib"), Txt(" f;\n")),
        Class("example", "A4", Reference("Thing", "lib"), Txt("a;\n")),
        Class("example", "A5", Reference("Thing", "lib"), Txt("\u00e9;\n")),
        Class("example", "A6", Reference("Thing", "lib"), Txt("\\;\n")));

    [Fact] void should_refuse_after_an_ascii_identifier_character() => Code(_result[0]).ShouldEqual(JavaRefusalCodes.SpellingContext);
    [Fact] void should_refuse_after_a_non_ascii_letter() => Code(_result[1]).ShouldEqual(JavaRefusalCodes.SpellingContext);
    [Fact] void should_refuse_after_a_backslash() => Code(_result[2]).ShouldEqual(JavaRefusalCodes.SpellingContext);
    [Fact] void should_refuse_before_an_ascii_identifier_character() => Code(_result[3]).ShouldEqual(JavaRefusalCodes.SpellingContext);
    [Fact] void should_refuse_before_a_non_ascii_letter() => Code(_result[4]).ShouldEqual(JavaRefusalCodes.SpellingContext);
    [Fact] void should_refuse_before_a_backslash() => Code(_result[5]).ShouldEqual(JavaRefusalCodes.SpellingContext);
}
