// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Languages.Java.for_JavaDeclarations;

public class when_a_members_parent_has_unknown_supertypes : given.a_declaration_plan
{
    bool _canShorten;

    void Establish()
    {
        SymbolSegment[] segments =
        [
            new DeclarationSegment(new("example", "Outer", []), "parent"),
            new DeclarationSegment(new("example", "Outer", ["Child"]), "child", ParentOwner: "parent") { Supertypes = KnownTypes<TypeName>.None },
        ];
        _declarations = new(new([new("file", "example", segments)], []));
    }

    void Because() => _canShorten = _declarations.CanShortenMember("file", "parent", new("example", "Outer", ["Child"]));

    [Fact] void should_not_treat_structural_parent_identity_as_complete_inheritance_evidence() => _canShorten.ShouldBeFalse();
}
