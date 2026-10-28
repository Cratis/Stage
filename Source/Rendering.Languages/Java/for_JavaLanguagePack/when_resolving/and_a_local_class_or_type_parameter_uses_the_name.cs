// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Declarative;
using Cratis.Stage.Rendering.Templates;
using Xunit;

namespace Cratis.Stage.Rendering.Languages.Java.for_JavaLanguagePack.when_resolving;

public class and_a_local_class_or_type_parameter_uses_the_name : given.a_java_pack
{
    ImmutableArray<DeclarativeFileResolution> _result;

    ImmutableArray<DeclarativeFileResolution> _parameter;

    void Because()
    {
        _result = Resolve(Class("example", "A", Txt("class Order {}\n"), Reference("Order", "lib"), Txt(" o;\n")));
        _parameter = Resolve(Class("example", "B", Txt("<Order> void m() {}\n"), Reference("Order", "lib"), Txt(" o;\n")));
    }

    [Fact] void should_qualify_the_foreign_type_that_a_local_class_would_hide() => Text(_result[0]).ShouldEqual(Expected("A", "class Order {}\nlib.Order o;\n"));
    [Fact] void should_qualify_the_foreign_type_that_a_type_parameter_would_hide() => Text(_parameter[0]).ShouldEqual(Expected("B", "<Order> void m() {}\nlib.Order o;\n"));
}
