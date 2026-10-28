// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Languages.Java.for_JavaPlan;

public class when_a_suffix_matches_an_unrenamed_sibling : given.a_java_plan
{
    JavaPlan _plan = null!;
    string _sibling = string.Empty;

    void Because()
    {
        _sibling = "Inner_" + Suffix("Outer.Inner");
        _plan = JavaPlan.Create([Java("Outer.java", "example", "Outer", "Outer.Inner", "Outer.inner", "Outer." + _sibling)]);
    }

    [Fact] void should_keep_the_uncontested_sibling() => Name(_plan, "Outer." + _sibling).ShouldEqual("Outer." + _sibling);
    [Fact] void should_grow_the_contested_suffix() => Name(_plan, "Outer.Inner").ShouldEqual("Outer.Inner_" + Suffix("Outer.Inner", 12));
}
