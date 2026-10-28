// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Languages.Java.for_JavaOutputIntegrity;

public class when_final_strings_contain_cross_segment_unicode_escapes : Specification
{
    Exception?[] _errors = [];

    void Because() => _errors = [.. new[] { @"\uE000", @"\ud800", @"\uE000", @"\u005c\\uE000" }
        .Select(text => Catch.Exception(() => JavaOutputIntegrity.Validate(text)))];

    [Fact] void should_refuse_private_use_from_joined_text_segments() => ((LanguagePackRefusal)_errors[0]!).Code.ShouldEqual(JavaRefusalCodes.PrivateUseCodePoint);
    [Fact] void should_refuse_a_lone_surrogate_from_joined_text_segments() => ((LanguagePackRefusal)_errors[1]!).Code.ShouldEqual(JavaRefusalCodes.LoneSurrogate);
    [Fact] void should_refuse_an_escape_completed_by_a_resolved_reference_name() => ((LanguagePackRefusal)_errors[2]!).Code.ShouldEqual(JavaRefusalCodes.PrivateUseCodePoint);
    [Fact] void should_refuse_private_use_when_imports_emitted_nothing_between_spans() => ((LanguagePackRefusal)_errors[3]!).Code.ShouldEqual(JavaRefusalCodes.PrivateUseCodePoint);
}
