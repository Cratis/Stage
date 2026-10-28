// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Declarative;
using Cratis.Stage.Rendering.Templates;
using Xunit;

namespace Cratis.Stage.Rendering.Languages.Java.for_JavaLanguagePack.when_resolving;

public class and_two_foreign_types_share_a_simple_name : given.a_java_pack
{
    ImmutableArray<DeclarativeFileResolution> _result;

    void Because() => _result = Resolve(Class("example", "A", Reference("Thing", "lib"), Txt(" a;\n"), Reference("Thing", "other"), Txt(" b;\n")));

    [Fact] void should_qualify_both_so_that_neither_wins() => Text(_result[0]).ShouldEqual(Expected("A", "lib.Thing a;\nother.Thing b;\n"));
}
