// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Languages.Java.for_JavaPlan;

public class when_a_symbol_has_an_invalid_segment : given.a_java_plan
{
    string[] _codes = [];

    void Because()
    {
        _codes = [.. new[] { "Outer$Inner", "Outer..Inner", "class", "_", "Caf\u00e9", "1Outer", "Outer." }
            .SelectMany(name => Codes(File("index.txt", Reference(name))).Concat(Codes(Java("Outer.java", "example", name)))),

            .. new[] { "example..nested", "example.class", "Example$", "_", ".example" }
            .SelectMany(package => Codes(File("index.txt", Reference("Outer", package))))];
    }

    [Fact] void should_validate_every_dotted_symbol_segment() => _codes.ShouldEachConformTo(code => code == JavaRefusalCodes.InvalidIdentifier);
}
