// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Languages.Java.for_JavaFileScope.when_choosing_spellings;

public class and_a_foreign_type_can_be_imported : given.a_file_scope
{
    void Because() => CreateScope(new("zeta", "Widget", []), new("Alpha.library", "lowercase", []), new("zeta", "Widget", []));

    [Fact] void should_use_the_simple_name() => _scope.Spell(new("zeta", "Widget", [])).ShouldEqual("Widget");
    [Fact] void should_keep_structured_package_and_type_boundaries() => _scope.Spell(new("Alpha.library", "lowercase", [])).ShouldEqual("lowercase");
    [Fact] void should_sort_and_deduplicate_the_imports() => JavaImportBlock.Render(_scope.Imports).ShouldEqual("import Alpha.library.lowercase;\nimport zeta.Widget;\n");
}
