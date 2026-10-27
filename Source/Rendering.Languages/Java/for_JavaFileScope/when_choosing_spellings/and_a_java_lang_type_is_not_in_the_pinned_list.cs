// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Languages.Java.for_JavaFileScope.when_choosing_spellings;

public class and_a_java_lang_type_is_not_in_the_pinned_list : given.a_file_scope
{
    void Because() => CreateScope(new TypeName("java.lang", "UnpinnedType", []));

    [Fact] void should_not_assume_an_implicit_binding() => _scope.Spell(new("java.lang", "UnpinnedType", [])).ShouldEqual("java.lang.UnpinnedType");
    [Fact] void should_not_add_a_speculative_import() => _scope.Imports.Count.ShouldEqual(0);
}
