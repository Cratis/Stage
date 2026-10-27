// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Languages.Java.for_JavaFileScope.when_choosing_spellings;

public class and_java_lang_obscures_a_package : given.a_file_scope
{
    void Because() => CreateScope(new TypeName("String.library", "Outer", ["Inner"]));

    [Fact] void should_not_qualify_through_an_implicit_type() => _scope.Spell(new("String.library", "Outer", ["Inner"]), "container").ShouldEqual("Inner");
    [Fact] void should_import_the_member() => JavaImportBlock.Render(_scope.Imports).ShouldEqual("import String.library.Outer.Inner;\n");
}
