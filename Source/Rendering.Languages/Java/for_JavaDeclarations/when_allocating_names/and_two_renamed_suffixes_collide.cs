// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Languages.Java.for_JavaDeclarations.when_allocating_names;

public class and_two_renamed_suffixes_collide : given.a_declaration_plan
{
    void Establish()
    {
        SymbolSegment[] segments =
        [
            new DeclarationSegment(new("example", "Foo", []), "suffix-owner-6001"),
            new DeclarationSegment(new("example", "Foo", []), "suffix-owner-107621"),
        ];
        _plan = new([new("file", "example", segments)], []);
    }

    void Because() => _declarations = new(_plan);

    [Fact] void should_disambiguate_both_generated_names() => _declarations.InFile("file").Select(type => type.Name).Distinct(StringComparer.OrdinalIgnoreCase).Count().ShouldEqual(2);
    [Fact] void should_retry_the_shared_sha256_prefix() => _declarations.InFile("file").ShouldEachConformTo(type => type.Name.StartsWith("Foo_808cc8ad_", StringComparison.Ordinal));
}
