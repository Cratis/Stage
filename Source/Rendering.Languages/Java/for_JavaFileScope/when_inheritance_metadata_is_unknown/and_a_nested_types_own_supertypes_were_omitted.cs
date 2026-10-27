// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Languages.Java.for_JavaFileScope.when_inheritance_metadata_is_unknown;

public class and_a_nested_types_own_supertypes_were_omitted : given.an_inheritance_scope
{
    void Establish() => _file = _file with
    {
        Segments =
        [
            new DeclarationSegment(new("p", "C", []), "C") { Supertypes = KnownTypes<TypeName>.None },
            new DeclarationSegment(new("p", "C", ["Inner"]), "inner", ParentOwner: "C"),
        ],
    };

    void Because() => CreateInheritedScope(new TypeName("java.lang", "String", []));

    [Fact] void should_not_reuse_the_outer_types_known_empty_metadata() => _scope.Spell(new("java.lang", "String", []), "inner").ShouldEqual("java.lang.String");
    [Fact] void should_still_allow_the_explicitly_known_outer_scope() => _scope.Spell(new("java.lang", "String", []), "C").ShouldEqual("String");
}
