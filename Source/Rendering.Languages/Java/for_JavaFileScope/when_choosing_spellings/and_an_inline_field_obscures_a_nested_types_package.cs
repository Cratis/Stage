// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Languages.Java.for_JavaFileScope.when_choosing_spellings;

public class and_an_inline_field_obscures_a_nested_types_package : given.a_file_scope
{
    void Establish() => _file = _file with { Segments = [new NativeSegment("java", "int com; class Inner {}", "native", "member", [])] };
    void Because() => CreateScope(new TypeName("com.library", "Outer", ["Inner"]));

    [Fact] void should_use_an_unobscured_enclosing_type_qualification() => _scope.Spell(new("com.library", "Outer", ["Inner"])).ShouldEqual("Outer.Inner");
    [Fact] void should_import_the_enclosing_type() => JavaImportBlock.Render(_scope.Imports).ShouldEqual("import com.library.Outer;\n");
}
