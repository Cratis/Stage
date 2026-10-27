// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Languages.Java.for_JavaFileScope.when_inheritance_metadata_is_unknown;

public class and_a_declarations_supertypes_were_omitted : given.an_inheritance_scope
{
    void Establish() => _file = _file with { Segments = [new DeclarationSegment(new("p", "C", []), "C")] };
    void Because() => CreateInheritedScope(new("java.lang", "String", []), new("foreign", "Widget", []));

    [Fact] void should_qualify_the_implicit_type() => _scope.Spell(new("java.lang", "String", []), "C").ShouldEqual("java.lang.String");
    [Fact] void should_not_trust_a_single_type_import() => _scope.Spell(new("foreign", "Widget", []), "C").ShouldEqual("foreign.Widget");
}
