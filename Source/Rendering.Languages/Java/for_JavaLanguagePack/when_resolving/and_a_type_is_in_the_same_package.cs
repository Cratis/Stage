// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Declarative;
using Cratis.Stage.Rendering.Templates;
using Xunit;

namespace Cratis.Stage.Rendering.Languages.Java.for_JavaLanguagePack.when_resolving;

public class and_a_type_is_in_the_same_package : given.a_java_pack
{
    ImmutableArray<DeclarativeFileResolution> _result;

    void Because() => _result = Resolve(Class("example", "A", Reference("B"), Txt(" f;\n")), Class("example", "B"));

    [Fact] void should_spell_it_short_without_an_import() => Text(_result[0]).ShouldEqual(Expected("A", "B f;\n"));
}
