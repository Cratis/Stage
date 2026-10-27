// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Languages.Java.for_JavaFileScope.when_choosing_spellings;

public class and_a_sibling_declares_a_member_with_the_same_name : given.a_file_scope
{
    void Establish() => _file = _file with
    {
        Segments =
        [
            new DeclarationSegment(new("example", "First", []), "first") { Supertypes = KnownTypes<TypeName>.None },
            new DeclarationSegment(new("example", "First", ["String"]), "member", ParentOwner: "first") { Supertypes = KnownTypes<TypeName>.None },
            new DeclarationSegment(new("example", "Second", []), "second") { Supertypes = KnownTypes<TypeName>.None },
        ],
    };

    void Because() => CreateScope(new("java.lang", "String", []), new("example", "First", ["String"]));

    [Fact] void should_not_shorten_a_foreign_type() => _scope.Spell(new("java.lang", "String", []), "second").ShouldEqual("java.lang.String");
    [Fact] void should_not_treat_a_sibling_as_an_enclosing_scope() => _scope.Spell(new("example", "First", ["String"]), "second").ShouldEqual("example.First.String");
    [Fact] void should_allow_the_proven_enclosing_identity() => _scope.Spell(new("example", "First", ["String"]), "first").ShouldEqual("String");
}
