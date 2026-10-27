// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Languages.Java.for_JavaFileScope.when_inheritance_metadata_is_unknown;

public class and_a_transitive_ancestors_supertypes_were_omitted : given.an_inheritance_scope
{
    void Establish()
    {
        var ancestor = new TypeName("lib", "Ancestor", []);
        _plan = _plan with
        {
            ExternalDeclarations =
            [
                new(_baseType, KnownTypes<string>.None) { Supertypes = KnownTypes<TypeName>.Of([ancestor]) },
                new(ancestor, KnownTypes<string>.None),
            ],
        };
    }

    void Because() => CreateInheritedScope(new("java.lang", "String", []), new("foreign", "Widget", []));

    [Fact] void should_propagate_unknown_metadata_through_the_closure() => _scope.Spell(new("java.lang", "String", []), "C").ShouldEqual("java.lang.String");
    [Fact] void should_not_trust_the_import() => _scope.Spell(new("foreign", "Widget", []), "C").ShouldEqual("foreign.Widget");
}
