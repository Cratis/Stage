// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Languages.Java.for_JavaLanguagePack;

public class when_a_literal_span_is_invalid_on_its_own : given.a_symbol_plan
{
    Exception?[] _errors = [];

    void Because()
    {
        SymbolPlan[] plans =
        [
            WithSegments(new TextSegment(@"\u00"), new TextSegment("41")),
            WithSegments(new NativeSegment("java", @"\u00", "owner", "member", []), new TextSegment("41")),
            WithSegments(new TextSegment("\ud83d"), new NativeSegment("java", "\ude00", "owner", "member", [])),
            WithSegments(new NativeSegment("java", @"\ud83d", "owner", "member", []), new TextSegment(@"\ude00")),
        ];
        _errors = [.. plans.Select(plan => Catch.Exception(() => JavaLanguagePack.Validate(plan)))];
    }

    [Fact] void should_refuse_an_incomplete_text_escape() => ((LanguagePackRefusal)_errors[0]!).Code.ShouldEqual(JavaRefusalCodes.MalformedUnicodeEscape);
    [Fact] void should_refuse_an_incomplete_native_escape() => ((LanguagePackRefusal)_errors[1]!).Code.ShouldEqual(JavaRefusalCodes.MalformedUnicodeEscape);
    [Fact] void should_refuse_a_raw_lone_surrogate_in_one_span() => ((LanguagePackRefusal)_errors[2]!).Code.ShouldEqual(JavaRefusalCodes.LoneSurrogate);
    [Fact] void should_refuse_a_decoded_lone_surrogate_in_one_span() => ((LanguagePackRefusal)_errors[3]!).Code.ShouldEqual(JavaRefusalCodes.LoneSurrogate);
}
