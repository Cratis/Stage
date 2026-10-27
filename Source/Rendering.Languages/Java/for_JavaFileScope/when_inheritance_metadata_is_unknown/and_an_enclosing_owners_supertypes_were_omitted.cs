// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Languages.Java.for_JavaFileScope.when_inheritance_metadata_is_unknown;

public class and_an_enclosing_owners_supertypes_were_omitted : given.an_inheritance_scope
{
    void Establish() => _file = _file with
    {
        Segments =
        [
            new DeclarationSegment(new("p", "C", []), "C"),
            new DeclarationSegment(new("p", "C", ["Inner"]), "inner", ParentOwner: "C") { Supertypes = KnownTypes<TypeName>.None },
        ],
    };

    void Because() => CreateInheritedScope(new("java.lang", "String", []), new("foreign", "Widget", []));

    [Fact] void should_not_let_known_empty_inner_metadata_hide_unknown_outer_metadata() => _scope.Spell(new("java.lang", "String", []), "inner").ShouldEqual("java.lang.String");
    [Fact] void should_qualify_the_import_in_the_inner_scope() => _scope.Spell(new("foreign", "Widget", []), "inner").ShouldEqual("foreign.Widget");
}
