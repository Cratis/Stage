// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Declarative;
using Cratis.Stage.Rendering.Templates;
using Xunit;

namespace Cratis.Stage.Rendering.Languages.Java.for_JavaLanguagePack.when_resolving;

public class and_a_supertype_member_type_is_named_like_the_enclosing_type : given.a_java_pack
{
    ImmutableArray<DeclarativeFileResolution> _result;

    void Because() => _result = Resolve(File("Outer.java", Imports(), Txt("public class "), Declaration("Outer"), Txt(" {\n  static class "), Declaration("Outer.Inner"), Txt(" {}\n  "), Reference("Shadowing", "lib"), Txt(" s;\n  "), Reference("Outer"), Txt(" self;\n}\n")));

    [Fact] void should_qualify_the_enclosing_type_because_the_inherited_member_type_would_hide_it() =>
        Text(_result[0]).ShouldEqual(Header("lib.Shadowing") + "public class Outer {\n  static class Inner {}\n  Shadowing s;\n  example.Outer self;\n}\n");
}
