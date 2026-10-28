// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Declarative;
using Cratis.Stage.Rendering.Templates;
using Xunit;

namespace Cratis.Stage.Rendering.Languages.Java.for_JavaLanguagePack.when_resolving;

public class and_an_annotation_opens_a_parenthesis : given.a_java_pack
{
    ImmutableArray<DeclarativeFileResolution> _result;

    void Because() => _result = Resolve(Class("example", "A", Txt("@Ann(x = "), Reference("Thing", "lib"), Txt(".class) int y;\n")));

    [Fact] void should_spell_the_reference_normally() => Text(_result[0]).ShouldEqual(Expected("A", "@Ann(x = Thing.class) int y;\n", "lib.Thing"));
}
