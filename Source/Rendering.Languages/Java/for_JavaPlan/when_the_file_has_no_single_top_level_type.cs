// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Languages.Java.for_JavaPlan;

public class when_the_file_has_no_single_top_level_type : given.a_java_plan
{
    string[] _codes = [];

    void Because() => _codes = [.. Codes(File("Outer.java", Imports())), .. Codes(Java("Outer.java", "example", "Outer", "Other"))];

    [Fact] void should_require_exactly_one_distinct_top_level_symbol() => _codes.ShouldEachConformTo(code => code == JavaRefusalCodes.FileStructure);
}
