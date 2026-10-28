// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Declarative;
using Cratis.Stage.Rendering.Templates;
using Xunit;

namespace Cratis.Stage.Rendering.Languages.Java.for_JavaLanguagePack.when_resolving;

public class and_the_enclosing_type_refers_to_itself_inside_a_nested_type : given.a_java_pack
{
    ImmutableArray<DeclarativeFileResolution> _result;

    void Because() => _result = Resolve(File("Outer.java", Imports(), Txt("public class "), Declaration("Outer"), Txt(" {\n  static class "), Declaration("Outer.Inner"), Txt(" {\n    "), Reference("Outer"), Txt(" parent;\n  }\n}\n")));

    [Fact] void should_spell_the_enclosing_type_short() => Text(_result[0]).ShouldEqual(Header() + "public class Outer {\n  static class Inner {\n    Outer parent;\n  }\n}\n");
}
