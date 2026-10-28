// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Declarative;
using Cratis.Stage.Rendering.Templates;
using Xunit;

namespace Cratis.Stage.Rendering.Languages.Java.for_JavaLanguagePack.when_resolving;

public class and_an_authored_body_uses_the_name : given.a_java_pack
{
    ImmutableArray<DeclarativeFileResolution> _result;

    void Because() => _result = Resolve(Class("example", "A", Txt("int a;\n"), new DeclarativeAuthoredSegment("R1", "Thing.run();\n"), Reference("Thing", "lib"), Txt(" f;\n")));

    [Fact] void should_count_the_authored_words_and_qualify() => Text(_result[0]).ShouldEqual(Expected("A", "int a;\nThing.run();\nlib.Thing f;\n"));
    [Fact] void should_emit_the_authored_text_verbatim_as_its_own_piece() => _result[0].Pieces.OfType<DeclarativeAuthoredPiece>().Single().Text.ShouldEqual("Thing.run();\n");
}
