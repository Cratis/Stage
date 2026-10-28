// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Languages.Java.for_JavaPlan;

public class when_a_renamed_parent_has_grandchildren : given.a_java_plan
{
    JavaPlan _plan = null!;

    void Because() => _plan = JavaPlan.Create([Java("Outer.java", "example", "Outer", "Outer.same", "Outer.Same", "Outer.same.Child", "Outer.same.Child.Leaf")]);

    [Fact] void should_use_the_allocated_parent_prefix() => Name(_plan, "Outer.same.Child").ShouldEqual("Outer.same_" + Suffix("Outer.same") + ".Child");
    [Fact] void should_rewrite_every_descendant_prefix() => Name(_plan, "Outer.same.Child.Leaf").ShouldEqual("Outer.same_" + Suffix("Outer.same") + ".Child.Leaf");
}
