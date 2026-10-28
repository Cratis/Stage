// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Declarative;
using Cratis.Stage.Rendering.Templates;
using Xunit;

namespace Cratis.Stage.Rendering.Languages.Java.for_JavaLanguagePack.when_resolving;

public class and_a_same_package_type_shares_a_name_with_a_foreign_type : given.a_java_pack
{
    ImmutableArray<DeclarativeFileResolution> _result;

    void Because() => _result = Resolve(Class("example", "A", Reference("Thing"), Txt(" a;\n"), Reference("Thing", "lib"), Txt(" b;\n")), Class("example", "Thing"));

    [Fact] void should_keep_the_same_package_type_short_and_qualify_the_foreign_one() => Text(_result[0]).ShouldEqual(Expected("A", "Thing a;\nlib.Thing b;\n"));
}
