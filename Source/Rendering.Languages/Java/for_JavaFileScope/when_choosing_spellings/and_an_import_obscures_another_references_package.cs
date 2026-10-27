// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Languages.Java.for_JavaFileScope.when_choosing_spellings;

public class and_an_import_obscures_another_references_package : given.a_file_scope
{
    JavaFileScope _reversed = null!;

    void Because()
    {
        CreateScope(new("other", "com", []), new("com.library", "Outer", ["Inner"]));
        _reversed = new(_file, new(new([_file], [])), [new("com.library", "Outer", ["Inner"]), new("other", "com", [])]);
    }

    [Fact] void should_avoid_the_obscured_package() => _scope.Spell(new("com.library", "Outer", ["Inner"])).ShouldEqual("Inner");
    [Fact] void should_reserve_fallback_imports_before_spelling() => JavaImportBlock.Render(_scope.Imports).ShouldEqual("import com.library.Outer.Inner;\nimport other.com;\n");
    [Fact] void should_have_no_reference_order_winner() => JavaImportBlock.Render(_scope.Imports).ShouldEqual(JavaImportBlock.Render(_reversed.Imports));
}
