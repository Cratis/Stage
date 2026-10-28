// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Declarative;
using Cratis.Stage.Rendering.Templates;
using Xunit;

namespace Cratis.Stage.Rendering.Languages.Java.for_JavaLanguagePack.when_resolving;

public class and_a_nested_type_is_referenced : given.a_java_pack
{
    ImmutableArray<DeclarativeFileResolution> _result;

    ImmutableArray<DeclarativeFileResolution> _obscured;

    static DeclarativeLanguageFile Outer() => File("Outer.java", Imports(), Txt("public class "), Declaration("Outer"), Txt(" {\n  static class "), Declaration("Outer.Inner"), Txt(" {}\n}\n"));

    void Because()
    {
        _result = Resolve(Outer(), Class("example", "Other", Reference("Outer.Inner"), Txt(" x;\n")));
        _obscured = Resolve(Outer(), Class("example", "Third", Txt("int Outer = 0;\n"), Reference("Outer.Inner"), Txt(" x;\n")));
    }

    [Fact] void should_spell_it_through_the_head_and_never_as_the_bare_member() => Text(_result[1]).ShouldEqual(Expected("Other", "Outer.Inner x;\n"));
    [Fact] void should_qualify_the_head_when_the_head_is_not_free() => Text(_obscured[1]).ShouldEqual(Expected("Third", "int Outer = 0;\nexample.Outer.Inner x;\n"));
}
