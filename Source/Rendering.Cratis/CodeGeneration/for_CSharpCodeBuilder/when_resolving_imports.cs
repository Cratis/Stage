// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.Naming.Imports;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.CodeGeneration.for_CSharpCodeBuilder;

public class when_resolving_imports : Specification
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

    [Fact] void should_drop_the_import_a_global_using_repeats() => _result.Contains("using FluentValidation;", StringComparison.Ordinal).ShouldBeFalse();
    [Fact] void should_import_the_type_namespace_above_the_namespace() => _result.ShouldEqual("using System.Text.RegularExpressions;\n\nnamespace Shop.Common;\n\npublic record Item(Regex Id);\n");
    [Fact] void should_not_leave_a_placeholder() => _result.Contains('\u0001', StringComparison.Ordinal).ShouldBeFalse();
    [Fact] void should_resolve_without_mutating_the_builder() => _builder.ToString().ShouldEqual(_result);
}
