// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Languages.Java.for_JavaPlan;

public class when_files_and_declarations_are_reordered : given.a_java_plan
{
    JavaPlan _first = null!;
    JavaPlan _second = null!;

    void Because()
    {
        _first = JavaPlan.Create([Java("Outer.java", "example", "Outer", "Outer.Same", "Outer.same", "Outer.same.Child"), Java("Other.java", "example", "Other", "Other.Inner")]);
        _second = JavaPlan.Create([Java("Other.java", "example", "Other", "Other.Inner"), Java("Outer.java", "example", "Outer", "Outer.same.Child", "Outer.same", "Outer.Same")]);
    }

    [Fact] void should_allocate_identically_regardless_of_visit_order() => _first.Declarations.ShouldEachConformTo(entry => _second.Declarations[entry.Key].Allocated == entry.Value.Allocated);
    [Fact] void should_keep_the_input_file_order_for_the_resolver() => _second.Files[0].Path.ShouldEqual("Other.java");
}
