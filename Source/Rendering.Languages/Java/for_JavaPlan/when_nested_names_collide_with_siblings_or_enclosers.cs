// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Languages.Java.for_JavaPlan;

public class when_nested_names_collide_with_siblings_or_enclosers : given.a_java_plan
{
    JavaPlan _plan = null!;

    void Because() => _plan = JavaPlan.Create([Java("Outer.java", "example", "Outer", "Outer.Same", "Outer.same", "Outer.Inner", "Outer.Inner.outer")]);

    [Fact] void should_hash_both_case_colliding_siblings() => Name(_plan, "Outer.Same").ShouldEqual("Outer.Same_" + Suffix("Outer.Same"));
    [Fact] void should_not_select_a_winner_by_visit_order() => Name(_plan, "Outer.same").ShouldEqual("Outer.same_" + Suffix("Outer.same"));
    [Fact] void should_hash_a_name_that_matches_any_encloser_ignoring_case() => Name(_plan, "Outer.Inner.outer").ShouldEqual("Outer.Inner.outer_" + Suffix("Outer.Inner.outer"));
    [Fact] void should_leave_the_top_level_name_unchanged() => Name(_plan, "Outer").ShouldEqual("Outer");
}
