// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Declarative;
using Cratis.Stage.Rendering.Templates;
using Xunit;

namespace Cratis.Stage.Rendering.Languages.Java.for_JavaLanguagePack.when_resolving;

public class and_record_instanceof_is_not_a_declaration : given.a_java_pack
{
    ImmutableArray<DeclarativeFileResolution> _result;

    void Because() => _result = Resolve(Class("example", "A", Txt("boolean b = record instanceof "), Reference("Thing", "lib"), Txt(";\n")));

    [Fact] void should_spell_the_reference_short() => Text(_result[0]).ShouldEqual(Expected("A", "boolean b = record instanceof Thing;\n", "lib.Thing"));
}
