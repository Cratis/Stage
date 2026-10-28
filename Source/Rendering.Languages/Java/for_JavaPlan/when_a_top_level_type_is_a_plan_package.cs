// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Languages.Java.for_JavaPlan;

public class when_a_top_level_type_is_a_plan_package : given.a_java_plan
{
    string[] _codes = [];

    void Because() => _codes =
    [
        .. Codes(Java("Outer.java", "example", "Outer"), Java("Other.java", "example.Outer", "Other")),
        .. Codes(Java("Outer.java", "example", "Outer"), Java("Other.java", "example.Outer.deeper", "Other"))
    ];

    [Fact] void should_refuse_the_type_package_clash() => _codes.ShouldEachConformTo(code => code == JavaRefusalCodes.PackageTypeClash);
}
