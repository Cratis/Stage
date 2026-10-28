// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Declarative;
using Cratis.Stage.Rendering.Templates;
using Xunit;

namespace Cratis.Stage.Rendering.Languages.Java.for_JavaLanguagePack.when_resolving;

public class and_a_non_java_file_references_a_type : given.a_java_pack
{
    ImmutableArray<DeclarativeFileResolution> _result;

    void Because() => _result = Resolve(
        File("Outer.java", Imports(), Txt("public class "), Declaration("Outer"), Txt(" {\n  static class "), Declaration("Outer.Inner"), Txt(" {}\n}\n")),
        File("index.txt", Txt("see "), Reference("Thing", "lib"), Txt(" and "), Reference("Outer.Inner")),
        File("README.md", Txt(@"\usepackage{example}"), Reference("Outer")));

    [Fact] void should_write_the_canonical_name_of_an_external_type_and_the_allocated_name_of_a_plan_type() => Text(_result[1]).ShouldEqual("see lib.Thing and example.Outer.Inner");
    [Fact] void should_keep_text_that_is_not_a_java_unicode_escape() => Text(_result[2]).ShouldEqual(@"\usepackage{example}example.Outer");
}
