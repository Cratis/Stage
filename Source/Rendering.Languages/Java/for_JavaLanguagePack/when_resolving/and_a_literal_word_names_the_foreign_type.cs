// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Declarative;
using Cratis.Stage.Rendering.Templates;
using Xunit;

namespace Cratis.Stage.Rendering.Languages.Java.for_JavaLanguagePack.when_resolving;

public class and_a_literal_word_names_the_foreign_type : given.a_java_pack
{
    ImmutableArray<DeclarativeFileResolution> _result;

    ImmutableArray<DeclarativeFileResolution> _lang;

    void Because()
    {
        _result = Resolve(Class("example", "A", Txt("Thing.run();\n"), Reference("Thing", "lib"), Txt(" f;\n")));
        _lang = Resolve(Class("example", "B", Txt("String.valueOf(1);\n"), Reference("String", "java.lang"), Txt(" s;\n")));
    }

    [Fact] void should_not_import_a_name_that_literal_text_uses() => Text(_result[0]).ShouldEqual(Expected("A", "Thing.run();\nlib.Thing f;\n"));
    [Fact] void should_not_import_a_java_lang_name_that_literal_text_uses() => Text(_lang[0]).ShouldEqual(Expected("B", "String.valueOf(1);\njava.lang.String s;\n"));
}
