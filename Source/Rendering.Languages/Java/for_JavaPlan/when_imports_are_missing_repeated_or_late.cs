// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Languages.Java.for_JavaPlan;

public class when_imports_are_missing_repeated_or_late : given.a_java_plan
{
    string[] _codes = [];

    void Because() => _codes =
    [
        .. Codes(File("Outer.java", Declaration("Outer"))),
        .. Codes(File("Outer.java", Imports(), Imports(), Declaration("Outer"))),
        .. Codes(File("Outer.java", Declaration("Outer"), Imports()))
    ];

    [Fact] void should_require_exactly_one_imports_segment_before_the_first_declaration() => _codes.ShouldEachConformTo(code => code == JavaRefusalCodes.FileStructure);
}
