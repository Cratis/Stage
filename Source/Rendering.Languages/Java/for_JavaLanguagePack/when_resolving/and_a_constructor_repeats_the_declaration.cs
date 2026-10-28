// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Declarative;
using Cratis.Stage.Rendering.Templates;
using Xunit;

namespace Cratis.Stage.Rendering.Languages.Java.for_JavaLanguagePack.when_resolving;

public class and_a_constructor_repeats_the_declaration : given.a_java_pack
{
    ImmutableArray<DeclarativeFileResolution> _result;

    void Because() => _result = Resolve(File("A.java", Imports(), Txt("public class "), Declaration("A"), Txt(" {\n    public "), Declaration("A"), Txt("() {}\n}\n")));

    [Fact] void should_spell_every_repetition_as_the_declared_name() => Text(_result[0]).ShouldEqual(Header() + "public class A {\n    public A() {}\n}\n");
}
