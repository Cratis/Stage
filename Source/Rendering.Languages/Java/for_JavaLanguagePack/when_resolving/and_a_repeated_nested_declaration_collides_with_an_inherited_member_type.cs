// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Declarative;
using Xunit;

namespace Cratis.Stage.Rendering.Languages.Java.for_JavaLanguagePack.when_resolving;

public class and_a_repeated_nested_declaration_collides_with_an_inherited_member_type : given.a_java_pack
{
    ImmutableArray<DeclarativeFileResolution> _inherited;
    ImmutableArray<DeclarativeFileResolution> _own;

    void Because()
    {
        _inherited = new JavaLanguagePack(JavaTypeFacts.Parse("""{"types":{"lib.HasInner":["Inner"]}}""")).Resolve(
            [File("Outer.java", Imports(), Txt("public class "), Declaration("Outer"), Txt(" {\n  static class "), Declaration("Outer.Inner"), Txt(" {}\n  static class Other extends "), Reference("HasInner", "lib"), Txt(" {\n    Object o = new "), Declaration("Outer.Inner"), Txt("();\n  }\n}\n"))]);
        _own = Resolve(File("Outer.java", Imports(), Txt("public class "), Declaration("Outer"), Txt(" {\n  static class "), Declaration("Outer.Inner"), Txt(" {\n    "), Declaration("Outer.Inner"), Txt("() {}\n  }\n}\n")));
    }

    [Fact] void should_refuse_a_repeat_that_an_inherited_member_type_of_the_same_name_would_capture() => Code(_inherited[0]).ShouldEqual(JavaRefusalCodes.Unspellable);
    [Fact] void should_still_write_a_repeat_whose_only_source_is_the_own_nested_declaration() => Text(_own[0]).ShouldEqual(Header() + "public class Outer {\n  static class Inner {\n    Inner() {}\n  }\n}\n");
}
