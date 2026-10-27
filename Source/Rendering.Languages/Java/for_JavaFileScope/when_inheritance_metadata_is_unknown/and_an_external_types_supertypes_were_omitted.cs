// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Languages.Java.for_JavaFileScope.when_inheritance_metadata_is_unknown;

public class and_an_external_types_supertypes_were_omitted : given.an_inheritance_scope
{
    void Establish() => _plan = _plan with { ExternalDeclarations = [new(_baseType, KnownTypes<string>.None)] };
    void Because() => CreateInheritedScope(new("java.lang", "String", []), new("foreign", "Widget", []));

    [Fact] void should_not_confuse_known_empty_members_with_known_empty_supertypes() => _scope.Spell(new("java.lang", "String", []), "C").ShouldEqual("java.lang.String");
    [Fact] void should_qualify_the_imported_type() => _scope.Spell(new("foreign", "Widget", []), "C").ShouldEqual("foreign.Widget");
}
