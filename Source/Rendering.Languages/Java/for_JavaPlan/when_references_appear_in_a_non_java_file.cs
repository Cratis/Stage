// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Languages.Java.for_JavaPlan;

public class when_references_appear_in_a_non_java_file : given.a_java_plan
{
    JavaPlan _plan = null!;

    void Because() => _plan = JavaPlan.Create([File("index.txt", Reference("Outer.Inner", "other"))]);

    [Fact] void should_keep_the_canonical_symbol_for_later_emission() => _plan.Files[0].Segments.Single().ShouldEqual(Reference("Outer.Inner", "other"));
    [Fact] void should_not_register_a_reference_as_a_declaration() => _plan.Declarations.ShouldBeEmpty();
}
