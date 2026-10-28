// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Languages.Java.for_JavaPlan;

public class when_a_symbol_has_no_package : given.a_java_plan
{
    string[] _codes = [];

    void Because() => _codes = [.. Codes(Java("Outer.java", string.Empty, "Outer")), .. Codes(File("index.txt", Reference("Outer", string.Empty)))];

    [Fact] void should_refuse_declarations_and_references_in_the_default_package() => _codes.ShouldEachConformTo(code => code == JavaRefusalCodes.DefaultPackage);
}
