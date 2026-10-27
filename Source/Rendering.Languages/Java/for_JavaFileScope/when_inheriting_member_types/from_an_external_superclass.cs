// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Languages.Java.for_JavaFileScope.when_inheriting_member_types;

public class from_an_external_superclass : given.an_inheritance_scope
{
    void Because() => CreateInheritedScope(new("java.lang", "String", []), new("foreign", "Widget", []));

    [Fact] void should_not_bind_java_lang_string_to_lib_base_string() => _scope.Spell(new("java.lang", "String", []), "C").ShouldEqual("java.lang.String");
    [Fact] void should_not_shorten_a_single_type_import_shadowed_by_an_inherited_member() => _scope.Spell(new("foreign", "Widget", []), "C").ShouldEqual("foreign.Widget");
    [Fact] void should_have_reserved_the_single_type_import() => JavaImportBlock.Render(_scope.Imports).ShouldEqual("import foreign.Widget;\n");
    [Fact] void should_be_conservative_when_the_lexical_owner_is_unspecified() => _scope.Spell(new("java.lang", "String", [])).ShouldEqual("java.lang.String");
}
