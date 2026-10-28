// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Languages.Java.for_JavaPlan;

public class when_a_child_name_matches_its_allocated_encloser : given.a_java_plan
{
    JavaPlan _plan = null!;
    string _child = string.Empty;

    void Because()
    {
        _child = "Same_" + Suffix("Outer.Same");
        _plan = JavaPlan.Create([Java("Outer.java", "example", "Outer", "Outer.Same", "Outer.same", "Outer.Same." + _child)]);
    }

    [Fact] void should_rename_against_allocated_as_well_as_original_encloser_names() => Name(_plan, "Outer.Same." + _child).ShouldEqual("Outer." + _child + "." + _child + "_" + Suffix("Outer.Same." + _child));
}
