// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Languages.Java.for_JavaFileScope.when_inheriting_member_types;

public class through_the_nested_types_own_supertype : given.an_inheritance_scope
{
    void Establish() => _file = _file with
    {
        Segments =
        [
            new DeclarationSegment(new("p", "C", []), "C") { Supertypes = KnownTypes<TypeName>.None },
            new DeclarationSegment(new("p", "C", ["Inner"]), "inner", ParentOwner: "C") { Supertypes = KnownTypes<TypeName>.Of([_baseType]) },
        ],
    };

    void Because() => CreateInheritedScope(new("java.lang", "String", []), new("foreign", "Widget", []));

    [Fact] void should_walk_the_inner_types_own_supertypes() => _scope.Spell(new("java.lang", "String", []), "inner").ShouldEqual("java.lang.String");
    [Fact] void should_reserve_the_import_only_in_the_inner_scope() => _scope.Spell(new("foreign", "Widget", []), "inner").ShouldEqual("foreign.Widget");
    [Fact] void should_not_invent_an_inheritance_edge_on_the_outer_type() => _scope.Spell(new("java.lang", "String", []), "C").ShouldEqual("String");
}
