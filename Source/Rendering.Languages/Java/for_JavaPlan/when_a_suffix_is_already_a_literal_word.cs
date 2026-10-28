// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Rendering.Declarative;
using Xunit;

namespace Cratis.Stage.Rendering.Languages.Java.for_JavaPlan;

public class when_a_suffix_is_already_a_literal_word : given.a_java_plan
{
    JavaPlan _plan = null!;

    void Because() => _plan = JavaPlan.Create([File("Outer.java", Imports(), Declaration("Outer"), Declaration("Outer.Inner"), Declaration("Outer.inner"), new DeclarativeTextSegment("/* Inner_" + Suffix("Outer.Inner") + " */"))]);

    [Fact] void should_grow_the_suffix_past_literal_words_including_comments() => Name(_plan, "Outer.Inner").ShouldEqual("Outer.Inner_" + Suffix("Outer.Inner", 12));
    [Fact] void should_not_grow_an_uncontested_siblings_suffix() => Name(_plan, "Outer.inner").ShouldEqual("Outer.inner_" + Suffix("Outer.inner"));
}
