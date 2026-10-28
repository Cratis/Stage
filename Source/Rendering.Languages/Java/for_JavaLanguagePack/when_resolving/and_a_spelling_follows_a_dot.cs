// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Declarative;
using Cratis.Stage.Rendering.Templates;
using Xunit;

namespace Cratis.Stage.Rendering.Languages.Java.for_JavaLanguagePack.when_resolving;

public class and_a_spelling_follows_a_dot : given.a_java_pack
{
    ImmutableArray<DeclarativeFileResolution> _result;

    void Because() => _result = Resolve(
        Class("example", "A1", Txt("x."), Reference("Thing", "lib"), Txt(" f;\n")),
        Class("example", "A2", Txt("x./**/"), Reference("Thing", "lib"), Txt(" f;\n")),
        Class("example", "A3", Txt("x.\n  "), Reference("Thing", "lib"), Txt(" f;\n")));

    [Fact] void should_refuse_a_member_access() => Code(_result[0]).ShouldEqual(JavaRefusalCodes.SpellingContext);
    [Fact] void should_refuse_a_member_access_across_a_block_comment() => Code(_result[1]).ShouldEqual(JavaRefusalCodes.SpellingContext);
    [Fact] void should_refuse_a_member_access_across_a_line_break() => Code(_result[2]).ShouldEqual(JavaRefusalCodes.SpellingContext);
}
