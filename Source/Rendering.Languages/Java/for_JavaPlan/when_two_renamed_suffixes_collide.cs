// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Languages.Java.for_JavaPlan;

public class when_two_renamed_suffixes_collide : given.a_java_plan
{
    const string First = "nEsTEdCOLliDIngNames";
    const string Second = "NestedColliDiNgNames";
    JavaPlan _plan = null!;

    void Because() => _plan = JavaPlan.Create([Java("Outer.java", "example", "Outer", "Outer." + First, "Outer." + Second)]);

    [Fact] void should_exercise_an_actual_eight_digit_hash_collision() => Suffix("Outer." + First).ShouldEqual(Suffix("Outer." + Second));
    [Fact] void should_grow_both_colliding_candidates() => Name(_plan, "Outer." + First).ShouldEqual("Outer." + First + "_" + Suffix("Outer." + First, 12));
    [Fact] void should_not_let_the_first_candidate_win() => Name(_plan, "Outer." + Second).ShouldEqual("Outer." + Second + "_" + Suffix("Outer." + Second, 12));
}
