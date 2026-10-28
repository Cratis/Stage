// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Languages.Java.for_JavaPlan;

public class when_a_required_suffix_would_exceed_the_identifier_limit : given.a_java_plan
{
    string[] _codes = [];

    void Because()
    {
        var name = new string('a', 250);
        _codes = [.. Codes(Java("Outer.java", "example", "Outer", "Outer." + name, "Outer." + name.ToUpperInvariant()))];
    }

    [Fact] void should_refuse_instead_of_truncating_or_emitting_an_invalid_identifier() => _codes.ShouldEqual([JavaRefusalCodes.InvalidIdentifier]);
}
