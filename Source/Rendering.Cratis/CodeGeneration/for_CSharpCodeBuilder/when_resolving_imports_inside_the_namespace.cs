// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.Naming.Imports;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.CodeGeneration.for_CSharpCodeBuilder;

public class when_resolving_imports_inside_the_namespace : Specification
{
    CSharpCodeBuilder _builder = null!;
    string _result = null!;

    void Establish()
    {
        _builder = new(new ImportRequest("Shop.Common", GeneratedProjects.Application, [], []));
        _builder.Using("FluentValidation").Namespace("Shop.Common")
            .Line($"public record Item({_builder.Type(new("System.Text.RegularExpressions", "Regex", 0, ReferenceContext.Type))} Id);");
    }

    void Because() => _result = _builder.ToString();

    [Fact] void should_retain_the_extension_import_at_the_top() => _result.ShouldContain("using FluentValidation;\n\nnamespace Shop.Common;");
    [Fact] void should_place_the_type_import_inside_the_namespace() => _result.ShouldContain("namespace Shop.Common;\n\nusing global::System.Text.RegularExpressions;\n\npublic record Item(Regex Id);");
    [Fact] void should_not_leave_a_placeholder() => _result.Contains('\u0001', StringComparison.Ordinal).ShouldBeFalse();
    [Fact] void should_resolve_without_mutating_the_builder() => _builder.ToString().ShouldEqual(_result);
}
