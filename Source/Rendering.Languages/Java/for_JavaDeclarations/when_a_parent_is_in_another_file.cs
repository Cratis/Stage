// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Languages.Java.for_JavaDeclarations;

public class when_a_parent_is_in_another_file : given.a_declaration_plan
{
    Exception _error = null!;

    void Establish() => _plan = new(
        [
            File("parent-file", "Outer", "parent"),
            new("child-file", "example", [new DeclarationSegment(new("example", "Outer", ["Child"]), "child", ParentOwner: "parent")]),
        ],
        []);

    void Because() => _error = Catch.Exception(() => _declarations = new(_plan));

    [Fact] void should_refuse_the_cross_file_parent_identity() => ((LanguagePackRefusal)_error).Code.ShouldEqual(JavaRefusalCodes.MissingDeclaration);
}
