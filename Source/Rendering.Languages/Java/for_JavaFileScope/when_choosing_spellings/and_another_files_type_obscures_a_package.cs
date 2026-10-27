// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Languages.Java.for_JavaFileScope.when_choosing_spellings;

public class and_another_files_type_obscures_a_package : given.a_file_scope
{
    void Because()
    {
        var other = new SymbolFile("other", "example", [new DeclarationSegment(new("example", "com", []), "other")]);
        _scope = new(_file, new(new([_file, other], [])), [new("com.library", "Outer", ["Inner"])]);
    }

    [Fact] void should_not_qualify_through_the_same_package_type() => _scope.Spell(new("com.library", "Outer", ["Inner"]), "container").ShouldEqual("Inner");
    [Fact] void should_import_the_member() => JavaImportBlock.Render(_scope.Imports).ShouldEqual("import com.library.Outer.Inner;\n");
}
