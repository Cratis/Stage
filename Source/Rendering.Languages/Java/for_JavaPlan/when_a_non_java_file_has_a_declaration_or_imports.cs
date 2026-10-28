// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Languages.Java.for_JavaPlan;

public class when_a_non_java_file_has_a_declaration_or_imports : given.a_java_plan
{
    string[] _codes = [];

    void Because() => _codes =
    [
        .. Codes(File("Outer.JAVA", Declaration("Outer"))),
        .. Codes(File("index.txt", Imports()))
    ];

    [Fact] void should_recognize_only_the_ordinal_lowercase_java_extension() => _codes.ShouldEachConformTo(code => code == JavaRefusalCodes.NotJavaSource);
}
