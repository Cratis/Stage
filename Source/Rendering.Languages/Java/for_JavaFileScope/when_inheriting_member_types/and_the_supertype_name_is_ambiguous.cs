// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Languages.Java.for_JavaFileScope.when_inheriting_member_types;

public class and_the_supertype_name_is_ambiguous : given.an_inheritance_scope
{
    void Establish() => _plan = new(
        [
            _file,
            new("first-base", "lib", [new DeclarationSegment(_baseType, "first") { Supertypes = KnownTypes<TypeName>.None }]),
            new("second-base", "lib", [new DeclarationSegment(_baseType, "second") { Supertypes = KnownTypes<TypeName>.None }]),
        ],
        []);

    void Because() => CreateInheritedScope(new("java.lang", "String", []), new("foreign", "Widget", []));

    [Fact] void should_qualify_instead_of_choosing_a_renamed_supertype() => _scope.Spell(new("java.lang", "String", []), "C").ShouldEqual("java.lang.String");
    [Fact] void should_not_trust_a_single_type_import() => _scope.Spell(new("foreign", "Widget", []), "C").ShouldEqual("foreign.Widget");
}
