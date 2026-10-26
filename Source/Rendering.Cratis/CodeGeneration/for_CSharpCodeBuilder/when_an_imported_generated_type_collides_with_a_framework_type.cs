// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.Naming.Imports;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.CodeGeneration.for_CSharpCodeBuilder;

public class when_an_imported_generated_type_collides_with_a_framework_type : Specification
{
    CSharpCodeBuilder _builder = null!;
    string _result = null!;

    void Establish()
    {
        _builder = new(new ImportRequest("Shop.Orders", GeneratedProjects.Application, [new("Shop.Common", "Guid")], []));
        _builder.Namespace("Shop.Orders").Line("public record Order(global::Shop.Common.Guid Key, global::System.Guid Id, global::System.DateTime Occurred);");
    }

    void Because() => _result = _builder.ToString();

    [Fact] void should_keep_the_generated_type_short() => _result.ShouldContain("Order(Guid Key, global::System.Guid Id, DateTime Occurred)");
    [Fact] void should_import_the_winning_namespace_inside_the_file_namespace() => _result.ShouldContain("namespace Shop.Orders;\n\nusing Shop.Common;");
    [Fact] void should_not_import_the_implicit_system_namespace() => _result.Contains("using System;", StringComparison.Ordinal).ShouldBeFalse();
}
