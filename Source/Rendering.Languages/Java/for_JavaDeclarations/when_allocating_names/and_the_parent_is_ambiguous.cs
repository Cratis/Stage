// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Languages.Java.for_JavaDeclarations.when_allocating_names;

public class and_the_parent_is_ambiguous : given.a_declaration_plan
{
    Exception _error = null!;

    void Establish()
    {
        SymbolSegment[] segments =
        [
            new DeclarationSegment(new("example", "Outer", []), "first"),
            new DeclarationSegment(new("example", "Outer", []), "second"),
            new DeclarationSegment(new("example", "Outer", ["Child"]), "child"),
        ];
        _plan = new([new("one", "example", segments)], []);
    }

    void Because() => _error = Catch.Exception(() => _declarations = new(_plan));

    [Fact] void should_refuse_to_guess() => ((LanguagePackRefusal)_error).Code.ShouldEqual(JavaRefusalCodes.AmbiguousDeclaration);
}
