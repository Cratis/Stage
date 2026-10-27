// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Languages.Java.for_JavaFileScope.when_inheriting_member_types;

public class through_a_known_empty_diamond : given.an_inheritance_scope
{
    void Establish()
    {
        var left = new TypeName("lib", "Left", []);
        var right = new TypeName("lib", "Right", []);
        var root = new TypeName("lib", "Root", []);
        _plan = _plan with
        {
            ExternalDeclarations =
            [
                new(_baseType, []) { Supertypes = [left, right] },
                new(left, []) { Supertypes = [root] },
                new(right, []) { Supertypes = [root] },
                new(root, []),
            ],
        };
    }

    void Because() => CreateInheritedScope(new("java.lang", "String", []), new("foreign", "Widget", []));

    [Fact] void should_not_confuse_a_shared_ancestor_with_a_cycle() => _scope.Spell(new("java.lang", "String", []), "C").ShouldEqual("String");
    [Fact] void should_allow_the_proven_import_binding() => _scope.Spell(new("foreign", "Widget", []), "C").ShouldEqual("Widget");
}
