// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Languages.Java.for_JavaPlan;

public class when_declarations_repeat_in_one_file : given.a_java_plan
{
    JavaPlan _plan = null!;

    void Because() => _plan = JavaPlan.Create([Java("Outer.java", "example", "Outer", "Outer", "Outer.Inner", "Outer.Inner")]);

    [Fact] void should_register_each_symbol_once() => _plan.Declarations.Count.ShouldEqual(2);
    [Fact] void should_keep_the_top_level_name() => Name(_plan, "Outer").ShouldEqual("Outer");
    [Fact] void should_keep_uncontested_nested_names() => Name(_plan, "Outer.Inner").ShouldEqual("Outer.Inner");
    [Fact] void should_keep_the_owning_file() => _plan.Declarations[new("example", "Outer.Inner")].Path.ShouldEqual("Outer.java");
}
