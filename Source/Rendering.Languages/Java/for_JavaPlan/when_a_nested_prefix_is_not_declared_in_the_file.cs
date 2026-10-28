// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Languages.Java.for_JavaPlan;

public class when_a_nested_prefix_is_not_declared_in_the_file : given.a_java_plan
{
    string[] _codes = [];

    void Because() => _codes =
    [
        .. Codes(Java("Outer.java", "example", "Outer", "Outer.Missing.Leaf")),
        .. Codes(Java("Outer.java", "example", "Outer", "Other.Inner"), Java("Other.java", "example", "Other")),
        .. Codes(File("Outer.java", Imports(), Declaration("Outer"), Declaration("Outer.Inner", "different")))
    ];

    [Fact] void should_require_the_immediate_prefix_with_the_same_package_in_the_same_file() => _codes.ShouldEachConformTo(code => code == JavaRefusalCodes.FileStructure);
}
