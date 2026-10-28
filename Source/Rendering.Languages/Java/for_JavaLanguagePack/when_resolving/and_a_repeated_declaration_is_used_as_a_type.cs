// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Declarative;
using Cratis.Stage.Rendering.Templates;
using Xunit;

namespace Cratis.Stage.Rendering.Languages.Java.for_JavaLanguagePack.when_resolving;

public class and_a_repeated_declaration_is_used_as_a_type : given.a_java_pack
{
    ImmutableArray<DeclarativeFileResolution> _inherited;
    ImmutableArray<DeclarativeFileResolution> _constructor;

    void Because()
    {
        _inherited = Resolve(File("Outer.java", Imports(), Txt("public class "), Declaration("Outer"), Txt(" {\n  "), Reference("Shadowing", "lib"), Txt(" s;\n  Object o = new "), Declaration("Outer"), Txt("();\n}\n")));
        _constructor = Resolve(Class("example", "B", Txt("  public "), Declaration("B", "example"), Txt("() {}\n")));
    }

    [Fact] void should_refuse_a_repeat_whose_name_an_inherited_member_type_would_capture() => Code(_inherited[0]).ShouldEqual(JavaRefusalCodes.Unspellable);
    [Fact] void should_still_write_a_constructor_repeat() => Text(_constructor[0]).ShouldEqual(Expected("B", "  public B() {}\n"));
}
