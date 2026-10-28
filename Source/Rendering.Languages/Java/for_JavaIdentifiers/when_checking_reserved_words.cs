// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Languages.Java.for_JavaIdentifiers;

public class when_checking_reserved_words : Specification
{
    bool[] _reserved = [];
    bool _ordinary;
    bool _different_case;

    void Because()
    {
        _reserved = [.. new[] { "class", "const", "goto", "true", "false", "null", "_", "record", "sealed", "non-sealed", "var", "yield", "when", "module" }.Select(JavaIdentifiers.IsReserved)];
        _ordinary = JavaIdentifiers.IsReserved("Customer");
        _different_case = JavaIdentifiers.IsReserved("Class");
    }

    [Fact] void should_protect_keywords_literals_and_contextual_words() => _reserved.ShouldEachConformTo(value => value);
    [Fact] void should_allow_an_ordinary_name() => _ordinary.ShouldBeFalse();
    [Fact] void should_compare_ordinally() => _different_case.ShouldBeFalse();
}
