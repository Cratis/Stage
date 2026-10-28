// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Declarative;
using Cratis.Stage.Rendering.Templates;
using Xunit;

namespace Cratis.Stage.Rendering.Languages.Java.for_JavaLanguagePack.when_resolving;

public class and_a_spelling_follows_a_line_comment : given.a_java_pack
{
    ImmutableArray<DeclarativeFileResolution> _result;

    void Because() => _result = Resolve(
        Class("example", "A1", Txt("x.\n// c\n"), Reference("Thing", "lib"), Txt(" f;\n")),
        Class("example", "A2", Txt("// c "), Reference("Thing", "lib"), Txt(" f;\n")));

    [Fact] void should_refuse_a_spelling_after_a_dot_and_a_comment_line() => Code(_result[0]).ShouldEqual(JavaRefusalCodes.SpellingContext);
    [Fact] void should_refuse_a_spelling_inside_a_comment() => Code(_result[1]).ShouldEqual(JavaRefusalCodes.SpellingContext);
}
