// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Languages.Java.for_JavaFileScope.when_inheritance_metadata_is_unknown;

public class and_text_declares_an_unmodeled_superclass : given.an_inheritance_scope
{
    void Establish() => _file = _file with
    {
        Segments =
        [
            new TextSegment("class "),
            new DeclarationSegment(new("p", "C", []), "C"),
            new TextSegment(" extends lib.Base {}"),
        ],
    };

    void Because() => CreateInheritedScope(new("java.lang", "String", []), new("foreign", "Widget", []));

    [Fact] void should_not_bind_string_to_lib_base_string() => _scope.Spell(new("java.lang", "String", []), "C").ShouldEqual("java.lang.String");
    [Fact] void should_qualify_imports_without_inventing_structural_metadata_from_text() => _scope.Spell(new("foreign", "Widget", []), "C").ShouldEqual("foreign.Widget");
}
