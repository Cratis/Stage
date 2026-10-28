// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Declarative;
using Cratis.Stage.Rendering.Templates;
using Xunit;

namespace Cratis.Stage.Rendering.Languages.Java.for_JavaLanguagePack.when_resolving;

public class and_a_supertype_inherits_a_member_type : given.a_java_pack
{
    ImmutableArray<DeclarativeFileResolution> _result;

    void Because() => _result = Resolve(Class("example", "A", Reference("Base", "lib"), Txt(" b;\n"), Reference("String", "java.lang"), Txt(" s;\n")));

    [Fact] void should_qualify_the_name_that_the_supertype_could_inherit() => Text(_result[0]).ShouldEqual(Expected("A", "Base b;\njava.lang.String s;\n", "lib.Base"));
}
