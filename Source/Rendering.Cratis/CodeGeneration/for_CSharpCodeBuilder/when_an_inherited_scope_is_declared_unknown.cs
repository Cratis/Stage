// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.Naming.Imports;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.CodeGeneration.for_CSharpCodeBuilder;

public class when_an_inherited_scope_is_declared_unknown : Specification
{
    CSharpCodeBuilder _builder = null!;
    string _result = null!;

    void Establish()
    {
        _builder = new(new ImportRequest("Shop.Orders", GeneratedProjects.Application, [], []));
        _builder.Inherits("Shop.External.Base");
        _builder.Namespace("Shop.Orders").Line("public record Order(global::System.Text.RegularExpressions.Regex Pattern);");
    }

    void Because() => _result = _builder.ToString();

    [Fact] void should_keep_the_generated_reference_qualified() => _result.ShouldEqual("namespace Shop.Orders;\n\npublic record Order(global::System.Text.RegularExpressions.Regex Pattern);\n");
    [Fact] void should_not_add_an_unused_import() => _result.Contains("using ", StringComparison.Ordinal).ShouldBeFalse();
}
