// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Languages.Java.for_JavaFileScope.when_choosing_spellings;

public class and_generated_text_declares_obscuring_names : given.a_file_scope
{
    void Establish() => _file = _file with { Segments = [new TextSegment("int \\u0063om; class List {} ")] };
    void Because() => CreateScope(new("com.library", "Outer", ["Inner"]), new("java.util", "List", []));

    [Fact] void should_not_qualify_through_the_generated_field() => _scope.Spell(new("com.library", "Outer", ["Inner"])).ShouldEqual("Inner");
    [Fact] void should_not_import_over_a_generated_member_type() => _scope.Spell(new("java.util", "List", [])).ShouldEqual("java.util.List");
    [Fact] void should_import_only_the_unobscured_nested_type() => JavaImportBlock.Render(_scope.Imports).ShouldEqual("import com.library.Outer.Inner;\n");
}
