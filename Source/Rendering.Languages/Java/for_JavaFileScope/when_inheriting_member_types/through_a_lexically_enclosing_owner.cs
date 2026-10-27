// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Languages.Java.for_JavaFileScope.when_inheriting_member_types;

public class through_a_lexically_enclosing_owner : given.an_inheritance_scope
{
    void Establish() => _file = _file with { Segments = [.. _file.Segments, new DeclarationSegment(new("p", "C", ["Inner"]), "inner", ParentOwner: "C") { Supertypes = KnownTypes<TypeName>.None }] };
    void Because() => CreateInheritedScope(new("java.lang", "String", []), new("foreign", "Widget", []));

    [Fact] void should_walk_the_outer_owners_supertypes() => _scope.Spell(new("java.lang", "String", []), "inner").ShouldEqual("java.lang.String");
    [Fact] void should_also_block_an_import_in_the_inner_scope() => _scope.Spell(new("foreign", "Widget", []), "inner").ShouldEqual("foreign.Widget");
}
