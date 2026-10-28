// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Languages.Java.for_JavaPlan;

public class when_the_first_declaration_is_not_the_file_type : given.a_java_plan
{
    string[] _codes = [];

    void Because() => _codes =
    [
        .. Codes(Java("Outer.java", "example", "Other")),
        .. Codes(Java("outer.java", "example", "Outer")),
        .. Codes(Java("Outer.java", "example", "Outer.Inner", "Outer"))
    ];

    [Fact] void should_require_a_matching_top_level_first_declaration() => _codes.ShouldEachConformTo(code => code == JavaRefusalCodes.FileStructure);
}
