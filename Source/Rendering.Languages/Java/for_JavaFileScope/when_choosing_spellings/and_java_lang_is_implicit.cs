// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Languages.Java.for_JavaFileScope.when_choosing_spellings;

public class and_java_lang_is_implicit : given.a_file_scope
{
    void Because() => CreateScope(new("java.lang", "String", []), new("foreign", "String", []), new("java.lang", "ScopedValue", []), new("java.lang", "StringTemplate", []));

    [Fact] void should_use_the_implicit_name() => _scope.Spell(new("java.lang", "String", []), "container").ShouldEqual("String");
    [Fact] void should_qualify_the_foreign_name() => _scope.Spell(new("foreign", "String", []), "container").ShouldEqual("foreign.String");
    [Fact] void should_include_pinned_preview_names() => _scope.Spell(new("java.lang", "ScopedValue", []), "container").ShouldEqual("ScopedValue");
    [Fact] void should_include_the_other_pinned_preview_name() => _scope.Spell(new("java.lang", "StringTemplate", []), "container").ShouldEqual("StringTemplate");
    [Fact] void should_not_emit_imports() => _scope.Imports.Count.ShouldEqual(0);
}
