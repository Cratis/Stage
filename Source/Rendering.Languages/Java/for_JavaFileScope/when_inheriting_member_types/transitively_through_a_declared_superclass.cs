// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Languages.Java.for_JavaFileScope.when_inheriting_member_types;

public class transitively_through_a_declared_superclass : given.an_inheritance_scope
{
    void Establish()
    {
        var middle = new TypeName("p", "B", []);
        _file = _file with { Segments = [new DeclarationSegment(new("p", "A", []), "A") { Supertypes = KnownTypes<TypeName>.Of([middle]) }] };
        var other = new SymbolFile("middle", "p", [new DeclarationSegment(middle, "B") { Supertypes = KnownTypes<TypeName>.Of([_baseType]) }]);
        _plan = _plan with { Files = [_file, other] };
    }

    void Because() => CreateInheritedScope(new("java.lang", "String", []), new("foreign", "Widget", []));

    [Fact] void should_walk_a_extends_b_extends_lib_base() => _scope.Spell(new("java.lang", "String", []), "A").ShouldEqual("java.lang.String");
    [Fact] void should_apply_transitive_shadowing_to_imports() => _scope.Spell(new("foreign", "Widget", []), "A").ShouldEqual("foreign.Widget");
}
