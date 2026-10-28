// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Languages.Java.for_JavaPlan;

public class when_names_are_the_same_in_different_scopes_or_packages : given.a_java_plan
{
    JavaPlan _plan = null!;

    void Because() => _plan = JavaPlan.Create([
        Java("one/Outer.java", "one", "Outer", "Outer.First", "Outer.Second", "Outer.First.Inner", "Outer.Second.inner"),
        Java("two/outer.java", "two", "outer")]);

    [Fact] void should_only_compare_siblings_in_the_same_immediate_scope() => Name(_plan, "Outer.First.Inner", "one").ShouldEqual("Outer.First.Inner");
    [Fact] void should_allow_top_level_case_variants_in_distinct_packages() => Name(_plan, "outer", "two").ShouldEqual("outer");
}
