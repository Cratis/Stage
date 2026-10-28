// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Declarative;
using Cratis.Stage.Rendering.Templates;
using Xunit;

namespace Cratis.Stage.Rendering.Languages.Java.for_JavaLanguagePack.when_resolving;

public class and_a_plan_supertype_declares_a_member_type : given.a_java_pack
{
    ImmutableArray<DeclarativeFileResolution> _result;

    static DeclarativeLanguageFile Base() => File("Base.java", Imports(), Txt("public class "), Declaration("Base"), Txt(" {\n  static class "), Declaration("Base.Item"), Txt(" {}\n}\n"));

    void Because() => _result = Resolve(Base(), Class("example", "A", Reference("Base"), Txt(" b;\n"), Reference("Item", "q"), Txt(" i;\n")));

    [Fact] void should_qualify_a_foreign_type_named_like_the_member_type() => Text(_result[1]).ShouldEqual(Expected("A", "Base b;\nq.Item i;\n"));
}
