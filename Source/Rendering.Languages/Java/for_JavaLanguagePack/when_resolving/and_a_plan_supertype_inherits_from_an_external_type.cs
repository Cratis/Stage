// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Declarative;
using Cratis.Stage.Rendering.Templates;
using Xunit;

namespace Cratis.Stage.Rendering.Languages.Java.for_JavaLanguagePack.when_resolving;

public class and_a_plan_supertype_inherits_from_an_external_type : given.a_java_pack
{
    ImmutableArray<DeclarativeFileResolution> _result;

    void Because() => _result = Resolve(
        Class("example", "Mid", Reference("Base", "lib"), Txt(" b;\n")),
        Class("example", "A", Reference("Mid"), Txt(" m;\n"), Reference("String", "java.lang"), Txt(" s;\n")));

    [Fact] void should_carry_the_external_inherited_names_through_the_plan_closure() => Text(_result[1]).ShouldEqual(Expected("A", "Mid m;\njava.lang.String s;\n"));
}
