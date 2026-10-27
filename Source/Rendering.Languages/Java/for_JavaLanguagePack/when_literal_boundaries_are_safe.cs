// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Languages.Java.for_JavaLanguagePack;

public class when_literal_boundaries_are_safe : given.a_symbol_plan
{
    Exception?[] _errors = [];

    void Because()
    {
        SymbolPlan[] plans =
        [
            WithSegments(new TextSegment("\\")),
            WithSegments(new TextSegment(@"\u005c")),
            WithSegments(new TextSegment(@"\u0041"), new ReferenceSegment(new("example", "uE000", []))),
            WithSegments(new TextSegment("\ud83d\ude00"), new NativeSegment("java", "body", "owner", "member", [])),
            WithSegments(new TextSegment(@"\ud83d\ude00"), new NativeSegment("java", "body", "owner", "member", [])),
            WithSegments(new TextSegment(@"\u005cx"), new ReferenceSegment(new("example", "uE000", []))),
            new([new("first", "example", [new TextSegment("\\")]), new("second", "example", [new TextSegment("uE000")])], []),
        ];
        _errors = [.. plans.Select(plan => Catch.Exception(() => JavaLanguagePack.Validate(plan)))];
    }

    [Fact] void should_allow_safe_complete_spans_and_trailing_backslashes_only_at_the_end_of_a_file() => _errors.ShouldEachConformTo(error => error == null);
}
