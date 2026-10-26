// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.Naming.Imports;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.CodeGeneration.for_CSharpCodeBuilder;

// The resolver reads one preprocessor branch, so a file with a conditional directive keeps every name qualified.
public class when_the_file_has_a_conditional_directive : Specification
{
    CSharpCodeBuilder _builder = null!;
    string _result = null!;

    void Establish()
    {
        _builder = new(new ImportRequest("Shop.Common", GeneratedProjects.Application, [], []));
        _builder.Namespace("Shop.Common").Line("#if DEBUG")
            .Line($"public record Item({_builder.Type(new("System.Text.RegularExpressions", "Regex", 0, ReferenceContext.Type))} Pattern);")
            .Line("#endif");
    }

    void Because() => _result = _builder.ToString();

    [Fact] void should_qualify_every_reference() => _result.ShouldContain("public record Item(global::System.Text.RegularExpressions.Regex Pattern);");
    [Fact] void should_not_add_imports() => _result.Contains("using ", StringComparison.Ordinal).ShouldBeFalse();
}
