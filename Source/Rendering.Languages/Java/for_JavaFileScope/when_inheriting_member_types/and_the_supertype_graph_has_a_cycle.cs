// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Languages.Java.for_JavaFileScope.when_inheriting_member_types;

public class and_the_supertype_graph_has_a_cycle : given.an_inheritance_scope
{
    void Establish()
    {
        var other = new TypeName("lib", "Other", []);
        _plan = _plan with { ExternalDeclarations = [new(_baseType, KnownTypes<string>.None) { Supertypes = KnownTypes<TypeName>.Of([other]) }, new(other, KnownTypes<string>.None) { Supertypes = KnownTypes<TypeName>.Of([_baseType]) }] };
    }

    void Because() => CreateInheritedScope(new("java.lang", "String", []), new("foreign", "Widget", []));

    [Fact] void should_terminate_and_qualify() => _scope.Spell(new("java.lang", "String", []), "C").ShouldEqual("java.lang.String");
    [Fact] void should_not_treat_the_cyclic_graph_as_known_empty() => _scope.Spell(new("foreign", "Widget", []), "C").ShouldEqual("foreign.Widget");
}
