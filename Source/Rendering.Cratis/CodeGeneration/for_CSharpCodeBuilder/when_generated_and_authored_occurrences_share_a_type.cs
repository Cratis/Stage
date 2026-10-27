// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.Naming.Imports;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.CodeGeneration.for_CSharpCodeBuilder;

public class when_generated_and_authored_occurrences_share_a_type : Specification
{
    CSharpCodeBuilder _builder = null!;
    string _result = null!;

    void Establish()
    {
        _builder = new(new ImportRequest("Shop.Orders", GeneratedProjects.Application, [], []));
        _builder.Namespace("Shop.Orders").OpenBlock("public class Order")
            .Raw("public global::System.Text.RegularExpressions.Regex Authored { get; init; }")
            .Line("public global::System.Text.RegularExpressions.Regex Generated { get; init; }")
            .EndBlock();
    }

    void Because() => _result = _builder.ToString();

    [Fact] void should_preserve_the_authored_qualification() => _result.ShouldContain("    public global::System.Text.RegularExpressions.Regex Authored { get; init; }\n");
    [Fact] void should_shorten_the_generated_occurrence_of_the_same_type() => _result.ShouldContain("    public Regex Generated { get; init; }\n");
    [Fact] void should_import_the_namespace_used_by_the_generated_occurrence() => _result.ShouldContain("using System.Text.RegularExpressions;\n");
}
