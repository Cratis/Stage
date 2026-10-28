// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Declarative;
using Cratis.Stage.Rendering.Templates;
using Xunit;

namespace Cratis.Stage.Rendering.Languages.Java.for_JavaLanguagePack.when_resolving;

public class and_a_foreign_type_is_named_like_a_type_with_nested_members : given.a_java_pack
{
    ImmutableArray<DeclarativeFileResolution> _result;

    void Because() => _result = Resolve(File("Outer.java", Imports(), Txt("public class "), Declaration("Outer"), Txt(" {\n  static class "), Declaration("Outer.Inner"), Txt(" {}\n  "), Reference("Outer"), Txt(" self;\n  "), Reference("Outer", "lib"), Txt(" other;\n}\n")));

    [Fact] void should_keep_the_declaring_package_type_short_and_qualify_the_foreign_type() =>
        Text(_result[0]).ShouldEqual(Header() + "public class Outer {\n  static class Inner {}\n  Outer self;\n  lib.Outer other;\n}\n");
}
