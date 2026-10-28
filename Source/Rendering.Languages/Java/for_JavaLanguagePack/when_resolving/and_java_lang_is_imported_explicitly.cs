// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Declarative;
using Cratis.Stage.Rendering.Templates;
using Xunit;

namespace Cratis.Stage.Rendering.Languages.Java.for_JavaLanguagePack.when_resolving;

public class and_java_lang_is_imported_explicitly : given.a_java_pack
{
    ImmutableArray<DeclarativeFileResolution> _result;

    void Because() => _result = Resolve(Class("example", "A", Reference("String", "java.lang"), Txt(" s;\n")));

    [Fact] void should_import_it_so_that_it_shadows_a_same_package_type_from_outside_the_plan() => Text(_result[0]).ShouldEqual(Expected("A", "String s;\n", "java.lang.String"));
}
