// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Declarative;
using Cratis.Stage.Rendering.Templates;
using Xunit;

namespace Cratis.Stage.Rendering.Languages.Java.for_JavaLanguagePack.when_resolving;

public class and_a_same_package_plan_type_shares_a_name_with_java_lang : given.a_java_pack
{
    ImmutableArray<DeclarativeFileResolution> _result;

    void Because() => _result = Resolve(Class("example", "A", Reference("String"), Txt(" a;\n"), Reference("String", "java.lang"), Txt(" b;\n")), Class("example", "String"));

    [Fact] void should_keep_the_plan_type_short_and_qualify_java_lang() => Text(_result[0]).ShouldEqual(Expected("A", "String a;\njava.lang.String b;\n"));
}
