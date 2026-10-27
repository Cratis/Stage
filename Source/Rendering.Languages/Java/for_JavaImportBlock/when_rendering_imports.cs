// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Languages.Java.for_JavaImportBlock;

public class when_rendering_imports : Specification
{
    string _block = string.Empty;

    void Because() => _block = JavaImportBlock.Render([new("zeta", "Order", []), new("Alpha", "lowercase", []), new("Alpha", "lowercase", []), new("alpha", "Outer", ["Inner"])], "\r\n");

    [Fact] void should_sort_ordinally_and_remove_duplicates() => _block.ShouldEqual("import Alpha.lowercase;\r\nimport alpha.Outer.Inner;\r\nimport zeta.Order;\r\n");
    [Fact] void should_not_use_wildcards() => _block.Contains('*').ShouldBeFalse();
    [Fact] void should_not_use_static_imports() => _block.Contains("import static", StringComparison.Ordinal).ShouldBeFalse();
}
