// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Languages.Java.for_JavaDeclarations;

public class when_an_external_type_is_claimed_as_parent : given.a_declaration_plan
{
    Exception _error = null!;

    void Establish() => _plan = new([new("file", "example", [new DeclarationSegment(new("example", "Outer", ["Child"]), "child", ParentOwner: "external:example.Outer")])], [new("example", "Outer", [])]);
    void Because() => _error = Catch.Exception(() => _declarations = new(_plan));

    [Fact] void should_not_graft_a_generated_member_onto_an_external_parent() => ((LanguagePackRefusal)_error).Code.ShouldEqual(JavaRefusalCodes.MissingDeclaration);
}
