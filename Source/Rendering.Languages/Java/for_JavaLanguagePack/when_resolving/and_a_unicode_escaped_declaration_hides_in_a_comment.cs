// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Declarative;
using Cratis.Stage.Rendering.Templates;
using Xunit;

namespace Cratis.Stage.Rendering.Languages.Java.for_JavaLanguagePack.when_resolving;

public class and_a_unicode_escaped_declaration_hides_in_a_comment : given.a_java_pack
{
    ImmutableArray<DeclarativeFileResolution> _result;

    void Because() => _result = Resolve(Class("example", "A", Txt("// \\u000a class Order {}\n"), Reference("Order", "lib"), Txt(" f;\n")));

    [Fact] void should_count_the_declared_name_as_a_word_and_qualify() => Text(_result[0]).ShouldEqual(Expected("A", "// \\u000a class Order {}\nlib.Order f;\n"));
}
