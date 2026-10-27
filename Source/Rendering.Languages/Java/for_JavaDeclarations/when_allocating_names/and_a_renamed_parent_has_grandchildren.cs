// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Languages.Java.for_JavaDeclarations.when_allocating_names;

public class and_a_renamed_parent_has_grandchildren : given.a_declaration_plan
{
    void Establish() => _plan = new(
        [
            new(
                "one",
                "example",
                [
                    new DeclarationSegment(new("example", "Outer", []), "parent"),
                    new DeclarationSegment(new("example", "Outer", ["Child"]), "child", ParentOwner: "parent"),
                    new DeclarationSegment(new("example", "Outer", ["Child", "Leaf"]), "leaf", ParentOwner: "child"),
                ]),
            File("two", "Outer", "other-parent"),
        ],
        []);

    void Because() => _declarations = new(_plan);

    [Fact] void should_bind_the_child_to_the_allocated_parent() => _declarations.Find(new("example", "Outer", ["Child"]), "child").Type!.Name.ShouldEqual(_declarations.Find(new("example", "Outer", []), "parent").Type!.Name);
    [Fact] void should_bind_the_grandchild_to_the_allocated_parent() => _declarations.Find(new("example", "Outer", ["Child", "Leaf"]), "leaf").Type!.Name.ShouldEqual(_declarations.Find(new("example", "Outer", []), "parent").Type!.Name);
    [Fact] void should_preserve_the_intermediate_identity() => _declarations.InFile("one").Single(type => type.Owner == "leaf").ParentOwner.ShouldEqual("child");
}
