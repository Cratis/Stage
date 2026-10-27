// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Languages.Java.for_JavaFileScope.when_inheriting_member_types;

public class from_a_generic_supertype : given.an_inheritance_scope
{
    void Establish()
    {
        _file = _file with { Segments = [.. _file.Segments, new TextSegment(" extends lib.Base<T> ")] };
        _plan = _plan with { ExternalDeclarations = [new(_baseType, KnownTypes<string>.Of(["String"])) { Supertypes = KnownTypes<TypeName>.None }] };
    }

    void Because() => CreateInheritedScope(new("java.lang", "String", []), new("foreign", "Widget", []));

    [Fact] void should_resolve_the_erased_base_type_and_find_its_member() => _scope.Spell(new("java.lang", "String", []), "C").ShouldEqual("java.lang.String");
    [Fact] void should_not_mistake_generic_arguments_for_an_unresolved_supertype_name() => _scope.Spell(new("foreign", "Widget", []), "C").ShouldEqual("Widget");
}
