// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Frozen;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Languages.Java.for_JavaLangTypes;

public class when_reading_the_jdk_21_baseline : Specification
{
    FrozenSet<string> _names = null!;

    void Because() => _names = JavaLangTypes.Names;

    [Fact] void should_include_scoped_value_preview() => _names.Contains("ScopedValue").ShouldBeTrue();
    [Fact] void should_include_string_template_preview() => _names.Contains("StringTemplate").ShouldBeTrue();
    [Fact] void should_include_match_exception_added_in_21() => _names.Contains("MatchException").ShouldBeTrue();
    [Fact] void should_include_string() => _names.Contains("String").ShouldBeTrue();
    [Fact] void should_exclude_later_jdk_additions() => _names.Contains("IO").ShouldBeFalse();
    [Fact] void should_compare_ordinally() => _names.Contains("string").ShouldBeFalse();
}
