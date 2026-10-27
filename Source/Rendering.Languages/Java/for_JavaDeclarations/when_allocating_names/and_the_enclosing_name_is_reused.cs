// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Languages.Java.for_JavaDeclarations.when_allocating_names;

public class and_the_enclosing_name_is_reused : given.a_declaration_plan
{
    void Establish()
    {
        SymbolSegment[] segments =
        [
            new DeclarationSegment(new("example", "Outer", []), "parent"),
            new DeclarationSegment(new("example", "Outer", ["Outer"]), "child", ParentOwner: "parent"),
        ];
        _plan = new([new("one", "example", segments)], []);
    }

    void Because() => _declarations = new(_plan);

    [Fact] void should_keep_the_parent_name() => _declarations.Find(new("example", "Outer", []), "parent").Type!.SimpleName.ShouldEqual("Outer");
    [Fact] void should_rename_the_member() => _declarations.Find(new("example", "Outer", ["Outer"]), "child").Type!.SimpleName.ShouldNotEqual("Outer");
}
