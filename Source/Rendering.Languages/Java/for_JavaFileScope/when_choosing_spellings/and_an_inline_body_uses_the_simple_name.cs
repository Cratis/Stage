// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Languages.Java.for_JavaFileScope.when_choosing_spellings;

public class and_an_inline_body_uses_the_simple_name : given.a_file_scope
{
    void Establish() => _file = _file with { Segments = [.. _file.Segments, new NativeSegment("java", "class \\u004cist {}", "native", "member", [])] };
    void Because() => CreateScope(new TypeName("java.util", "List", []));

    [Fact] void should_reserve_the_decoded_inline_declaration() => _scope.Spell(new("java.util", "List", [])).ShouldEqual("java.util.List");
    [Fact] void should_not_import_over_a_native_binding() => _scope.Imports.Count.ShouldEqual(0);
}
