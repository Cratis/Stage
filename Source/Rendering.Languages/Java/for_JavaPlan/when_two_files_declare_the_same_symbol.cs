// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Languages.Java.for_JavaPlan;

public class when_two_files_declare_the_same_symbol : given.a_java_plan
{
    string[] _codes = [];

    void Because() => _codes = [.. Codes(Java("one/Outer.java", "example", "Outer"), Java("two/Outer.java", "example", "Outer"))];

    [Fact] void should_refuse_instead_of_renaming_a_top_level_symbol() => _codes.ShouldEqual([JavaRefusalCodes.DuplicateDeclaration]);
}
