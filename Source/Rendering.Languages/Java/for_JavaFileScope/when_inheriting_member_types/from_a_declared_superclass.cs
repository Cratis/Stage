// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Languages.Java.for_JavaFileScope.when_inheriting_member_types;

public class from_a_declared_superclass : given.an_inheritance_scope
{
    void Establish()
    {
        SymbolSegment[] members =
        [
            new DeclarationSegment(_baseType, "base"),
            new DeclarationSegment(new("lib", "Base", ["String"]), "string", ParentOwner: "base"),
            new DeclarationSegment(new("lib", "Base", ["Widget"]), "widget", ParentOwner: "base"),
        ];
        _plan = new([_file, new("base-file", "lib", members)], []);
    }

    void Because() => CreateInheritedScope(new("java.lang", "String", []), new("foreign", "Widget", []));

    [Fact] void should_reserve_declared_member_type_names() => _scope.Spell(new("java.lang", "String", []), "C").ShouldEqual("java.lang.String");
    [Fact] void should_not_use_a_shadowed_import() => _scope.Spell(new("foreign", "Widget", []), "C").ShouldEqual("foreign.Widget");
}
