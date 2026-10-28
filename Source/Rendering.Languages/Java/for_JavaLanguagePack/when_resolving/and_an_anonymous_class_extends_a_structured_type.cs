// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Declarative;
using Cratis.Stage.Rendering.Templates;
using Xunit;

namespace Cratis.Stage.Rendering.Languages.Java.for_JavaLanguagePack.when_resolving;

public class and_an_anonymous_class_extends_a_structured_type : given.a_java_pack
{
    ImmutableArray<DeclarativeFileResolution> _result;

    void Because() => _result = Resolve(Class("example", "A", Txt("Object o = new "), Reference("Base", "lib"), Txt("() {\n    "), Reference("String", "java.lang"), Txt(" s;\n};\n")));

    [Fact] void should_qualify_a_name_that_the_anonymous_body_inherits() => Text(_result[0]).ShouldEqual(Expected("A", "Object o = new Base() {\n    java.lang.String s;\n};\n", "lib.Base"));
}
