// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Languages.Java.for_JavaPlan;

public class when_top_level_names_or_packages_differ_only_in_case : given.a_java_plan
{
    string[] _codes = [];

    void Because() => _codes =
    [
        .. Codes(Java("Outer.java", "example", "Outer"), Java("outer.java", "example", "outer")),
        .. Codes(Java("Outer.java", "example", "Outer"), Java("Other.java", "Example", "Other")),
        .. Codes(Java("Outer.java", "example.one", "Outer"), Java("Other.java", "Example.two", "Other"))
    ];

    [Fact] void should_refuse_case_collisions_across_the_plan() => _codes.ShouldEachConformTo(code => code == JavaRefusalCodes.CaseCollision);
}
