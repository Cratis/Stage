// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Languages.Java.for_JavaFileScope.when_inheriting_member_types;

public class transitively_through_an_external_superclass : given.an_inheritance_scope
{
    void Establish()
    {
        var ancestor = new TypeName("lib", "Ancestor", []);
        _plan = _plan with { ExternalDeclarations = [new(_baseType, []) { Supertypes = [ancestor] }, new(ancestor, ["String", "Widget"])] };
    }

    void Because() => CreateInheritedScope(new("java.lang", "String", []), new("foreign", "Widget", []));

    [Fact] void should_walk_external_supertype_edges() => _scope.Spell(new("java.lang", "String", []), "C").ShouldEqual("java.lang.String");
    [Fact] void should_qualify_shadowed_imports() => _scope.Spell(new("foreign", "Widget", []), "C").ShouldEqual("foreign.Widget");
}
