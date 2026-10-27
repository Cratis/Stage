// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Languages.Java.for_JavaFileScope.when_inheritance_metadata_is_unknown;

public class and_there_is_no_structured_declaration : given.an_inheritance_scope
{
    void Establish() => _file = _file with { Segments = [new TextSegment("class C extends lib.Base {}")] };
    void Because() => CreateInheritedScope(new TypeName("java.lang", "String", []));

    [Fact] void should_not_assume_an_empty_owner_chain() => _scope.Spell(new("java.lang", "String", [])).ShouldEqual("java.lang.String");
    [Fact] void should_not_assume_a_missing_owner_has_no_supertypes() => _scope.Spell(new("java.lang", "String", []), "C").ShouldEqual("java.lang.String");
}
