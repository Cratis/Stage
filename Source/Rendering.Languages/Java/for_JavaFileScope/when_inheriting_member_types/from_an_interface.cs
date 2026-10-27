// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Languages.Java.for_JavaFileScope.when_inheriting_member_types;

public class from_an_interface : given.an_inheritance_scope
{
    void Establish()
    {
        var contract = new TypeName("lib", "Contract", []);
        _file = _file with { Segments = [new DeclarationSegment(new("p", "C", []), "C") { Supertypes = KnownTypes<TypeName>.Of([contract]) }] };
        _plan = _plan with { ExternalDeclarations = [new(contract, KnownTypes<string>.Of(["String", "Widget"])) { Supertypes = KnownTypes<TypeName>.None }] };
    }

    void Because() => CreateInheritedScope(new("java.lang", "String", []), new("foreign", "Widget", []));

    [Fact] void should_qualify_the_implicit_type() => _scope.Spell(new("java.lang", "String", []), "C").ShouldEqual("java.lang.String");
    [Fact] void should_qualify_the_imported_type() => _scope.Spell(new("foreign", "Widget", []), "C").ShouldEqual("foreign.Widget");
}
