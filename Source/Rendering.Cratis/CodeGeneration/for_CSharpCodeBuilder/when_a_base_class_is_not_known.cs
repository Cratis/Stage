// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.Naming.Imports;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.CodeGeneration.for_CSharpCodeBuilder;

// A base class whose members are not known could declare a nested type or member named like any reference.
public class when_a_base_class_is_not_known : Specification
{
    CSharpCodeBuilder _builder = null!;
    string _result = null!;

    void Establish()
    {
        _builder = new(new ImportRequest("Shop.Common", GeneratedProjects.Application, [], []));
        _builder.Namespace("Shop.Common")
            .Line($"public class Item : global::Shop.External.Base {{ public {_builder.Type(new("System.Text.RegularExpressions", "Regex", 0, ReferenceContext.Type))} Pattern {{ get; init; }} }}");
    }

    void Because() => _result = _builder.ToString();

    [Fact] void should_qualify_every_reference() => _result.ShouldContain("public global::System.Text.RegularExpressions.Regex Pattern");
    [Fact] void should_not_add_imports() => _result.Contains("using ", StringComparison.Ordinal).ShouldBeFalse();
}
