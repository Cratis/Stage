// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Languages.Java.for_JavaFileScope.when_inheriting_member_types;

public class and_the_member_set_is_known_empty : given.an_inheritance_scope
{
    void Establish() => _plan = _plan with { ExternalDeclarations = [new(_baseType, KnownTypes<string>.None) { Supertypes = KnownTypes<TypeName>.None }] };
    void Because() => CreateInheritedScope(new("java.lang", "String", []), new("foreign", "Widget", []));

    [Fact] void should_allow_the_proven_implicit_binding() => _scope.Spell(new("java.lang", "String", []), "C").ShouldEqual("String");
    [Fact] void should_allow_the_proven_import_binding() => _scope.Spell(new("foreign", "Widget", []), "C").ShouldEqual("Widget");
}
