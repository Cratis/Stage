// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Languages.Java.for_JavaDeclarations.when_allocating_names;

public class and_an_external_type_reserves_the_name : given.a_declaration_plan
{
    void Establish() => _plan = new([File("one", "Order", "generated")], [new("example", "Order", [])]);
    void Because() => _declarations = new(_plan);

    [Fact] void should_rename_only_the_generated_type() => _declarations.InFile("one").Single().Name.ShouldNotEqual("Order");
    [Fact] void should_preserve_the_external_type() => _declarations.Find(new("example", "Order", []), "external:example.Order").Type!.SimpleName.ShouldEqual("Order");
}
