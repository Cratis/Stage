// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Declarative;
using Cratis.Stage.Rendering.Templates;
using Xunit;

namespace Cratis.Stage.Rendering.Languages.Java.for_JavaLanguagePack.when_resolving;

public class and_a_declaration_is_split_across_text_segments : given.a_java_pack
{
    ImmutableArray<DeclarativeFileResolution> _result;

    void Because() => _result = Resolve(Class("example", "A", Txt("cl"), Txt("ass Hidden {}\n"), Reference("Hidden", "lib"), Txt(" h;\n")));

    [Fact] void should_see_the_assembled_declaration_and_qualify() => Text(_result[0]).ShouldEqual(Expected("A", "class Hidden {}\nlib.Hidden h;\n"));
}
