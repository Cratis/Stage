// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Rendering.Declarative;
using Xunit;

namespace Cratis.Stage.Rendering.Languages.Java.for_JavaPlan;

public class when_assembled_literal_and_authored_text_reserves_a_suffix : given.a_java_plan
{
    JavaPlan _plan = null!;

    void Because()
    {
        var suffix = Suffix("Outer.Inner");
        _plan = JavaPlan.Create([File("Outer.java", Imports(), Declaration("Outer"), Declaration("Outer.Inner"), Declaration("Outer.inner"), new DeclarativeTextSegment("Inner_" + suffix[..4]), new DeclarativeAuthoredSegment("body", suffix[4..]))]);
    }

    [Fact] void should_scan_assembled_text_across_segment_boundaries() => Name(_plan, "Outer.Inner").ShouldEqual("Outer.Inner_" + Suffix("Outer.Inner", 12));
}
