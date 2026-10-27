// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Languages.Java.for_JavaFileScope.when_choosing_spellings;

public class and_a_member_import_avoids_an_obscured_package : given.a_file_scope
{
    void Establish() => _file = _file with { Segments = [.. _file.Segments, new NativeSegment("java", "int com;", "native", "statement", [])] };
    void Because() => CreateScope(new TypeName("com.library", "Outer", ["Inner"]));

    [Fact] void should_use_the_imported_member_name() => _scope.Spell(new("com.library", "Outer", ["Inner"]), "container").ShouldEqual("Inner");
    [Fact] void should_emit_a_single_type_member_import() => JavaImportBlock.Render(_scope.Imports).ShouldEqual("import com.library.Outer.Inner;\n");
}
