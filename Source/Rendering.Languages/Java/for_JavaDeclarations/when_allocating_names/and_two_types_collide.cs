// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Languages.Java.for_JavaDeclarations.when_allocating_names;

public class and_two_types_collide : given.a_declaration_plan
{
    void Establish() => _plan = new([File("one", "Order", "first"), File("two", "order", "second")], []);
    void Because() => _declarations = new(_plan);

    [Fact] void should_rename_both_symmetrically() => _declarations.InFile("one").Concat(_declarations.InFile("two")).All(type => type.Name.Contains('_')).ShouldBeTrue();
    [Fact] void should_allocate_distinct_names() => _declarations.InFile("one").Single().Name.ShouldNotEqual(_declarations.InFile("two").Single().Name);
    [Fact] void should_bind_a_reference_by_owner() => _declarations.Find(new("example", "Order", []), "first").Type!.QualifiedName.ShouldEqual(_declarations.InFile("one").Single().Final.QualifiedName);
}
