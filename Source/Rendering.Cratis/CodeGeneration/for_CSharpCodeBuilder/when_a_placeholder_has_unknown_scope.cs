// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.Naming.Imports;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.CodeGeneration.for_CSharpCodeBuilder;

public class when_a_placeholder_has_unknown_scope : Specification
{
    CSharpCodeBuilder _builder = null!;
    string _result = null!;

    void Establish()
    {
        _builder = new(new ImportRequest("Shop.Common", GeneratedProjects.Application, [], []));
        _builder.Namespace("Shop.Common").Line("// \u0001unknown\u0002")
            .Line($"public record Item({_builder.Type(new("System", "Guid", 0, ReferenceContext.Type))} Id);");
    }

    void Because() => _result = _builder.ToString();

    [Fact] void should_qualify_every_reference() => _result.ShouldContain("public record Item(global::System.Guid Id);");
    [Fact] void should_not_add_imports() => _result.Contains("using ", StringComparison.Ordinal).ShouldBeFalse();
}
