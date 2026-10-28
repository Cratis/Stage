// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Rendering.Declarative;
using Xunit;

namespace Cratis.Stage.Rendering.Languages.Java.for_JavaPlan;

public class when_package_words_appear_after_the_first_declaration : given.a_java_plan
{
    JavaPlan _plan = null!;

    void Because() => _plan = JavaPlan.Create([File("Outer.java", Imports(), Declaration("Outer"), new DeclarativeTextSegment("/* package import */"))]);

    [Fact] void should_only_check_the_header_for_literal_package_and_import_words() => _plan.Declarations.Count.ShouldEqual(1);
}
