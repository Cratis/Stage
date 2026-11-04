// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.Naming;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Naming.for_SemanticInlineNames.when_allocating_with_a_qualified_stem;

public class with_colliding_stems : Specification
{
    readonly SemanticInlineNameSite[] _sites =
    [
        new("a", "OrderPlacedTransition", "req-a", "reducer/a", QualifiedStem: "FirstOrderPlacedTransition"),
        new("b", "OrderPlacedTransition", "req-b", "reducer/a", QualifiedStem: "SecondOrderPlacedTransition"),
        new("alone", "OrderPlacedTransition", "req-c", "reducer/b", QualifiedStem: "ThirdOrderPlacedTransition"),
        new("same-a", "Tied", "req-d", "reducer/c", QualifiedStem: "Qualified"),
        new("same-b", "Tied", "req-e", "reducer/c", QualifiedStem: "Qualified")
    ];
    SemanticInlineNames _names = null!;
    SemanticInlineNames _reversed = null!;

    void Because()
    {
        _names = new(_sites, []);
        _reversed = new(_sites.Reverse(), []);
    }

    [Fact] void should_use_the_qualified_stem_for_the_first_colliding_site() => _names["a"].ShouldEqual("FirstOrderPlacedTransition");
    [Fact] void should_use_the_qualified_stem_for_the_second_colliding_site() => _names["b"].ShouldEqual("SecondOrderPlacedTransition");
    [Fact] void should_keep_the_plain_stem_when_it_is_unique_in_its_scope() => _names["alone"].ShouldEqual("OrderPlacedTransition");
    [Fact] void should_fall_back_to_an_identity_suffix_when_the_qualified_stem_also_collides() => Assert.All(["same-a", "same-b"], key => Assert.StartsWith("Qualified_", _names[key], StringComparison.Ordinal));
    [Fact] void should_not_depend_on_traversal_order() => Assert.All(_sites, site => _names[site.Key].ShouldEqual(_reversed[site.Key]));
}
