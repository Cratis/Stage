// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Languages.Java.for_JavaImportBlock;

public class when_rendering_imports : Specification
{
    string _none = string.Empty;
    string _several = string.Empty;

    void Because()
    {
        _none = JavaImportBlock.Render("example.orders", []);
        _several = JavaImportBlock.Render("example.orders", ["zeta.Order", "Alpha.lowercase", "java.lang.String", "Alpha.lowercase", "alpha.Outer.Inner"]);
    }

    [Fact] void should_end_a_block_without_imports_after_the_blank_line() => _none.ShouldEqual("package example.orders;\n\n");
    [Fact] void should_sort_ordinally_and_remove_duplicates() => _several.ShouldEqual("package example.orders;\n\nimport Alpha.lowercase;\nimport alpha.Outer.Inner;\nimport java.lang.String;\nimport zeta.Order;\n\n");
    [Fact] void should_not_use_wildcards() => _several.Contains('*').ShouldBeFalse();
    [Fact] void should_not_use_static_imports() => _several.Contains("import static", StringComparison.Ordinal).ShouldBeFalse();
}
