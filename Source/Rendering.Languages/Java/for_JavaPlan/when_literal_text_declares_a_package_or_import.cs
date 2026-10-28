// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Rendering.Declarative;
using Xunit;

namespace Cratis.Stage.Rendering.Languages.Java.for_JavaPlan;

public class when_literal_text_declares_a_package_or_import : given.a_java_plan
{
    string[] _codes = [];

    void Because() => _codes =
    [
        .. Codes(File("Outer.java", new DeclarativeTextSegment("pack"), new DeclarativeTextSegment("age example;"), Imports(), Declaration("Outer"))),
        .. Codes(File("Outer.java", Imports(), new DeclarativeTextSegment(@"\u0069mport other.Type;"), Declaration("Outer"))),
        .. Codes(File("Outer.java", Imports(), new DeclarativeTextSegment("/* package */"), Declaration("Outer")))
    ];

    [Fact] void should_check_the_decoded_assembled_prefix_without_parsing_comments() => _codes.ShouldEachConformTo(code => code == JavaRefusalCodes.FileStructure);
}
