// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Languages.Java.for_JavaFileScope.when_choosing_spellings;

public class and_an_inline_local_obscures_a_package : given.a_file_scope
{
    void Establish() => _file = _file with { Segments = [new NativeSegment("java", "int \\u0063om = 0;", "native", "statement", [])] };
    void Because() => CreateScope(new TypeName("com.library", "Widget", []));

    [Fact] void should_not_emit_an_obscured_qualified_name() => _scope.Spell(new("com.library", "Widget", [])).ShouldEqual("Widget");
    [Fact] void should_import_the_binding() => JavaImportBlock.Render(_scope.Imports).ShouldEqual("import com.library.Widget;\n");
}
