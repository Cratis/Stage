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
            WithSegments(new TextSegment(@"\\"), new ReferenceSegment(new("example", "uE000", []))),
            WithSegments(new TextSegment("\\"), new TextSegment("\\"), new ReferenceSegment(new("example", "uE000", []))),
            WithSegments(new TextSegment("\\")),
            WithSegments(new TextSegment(@"\u00"), new NativeSegment("java", "41", "owner", "member", [])),
            WithSegments(new TextSegment("\ud83d"), new NativeSegment("java", "\ude00", "owner", "member", [])),
            WithSegments(new TextSegment(@"\ud83d"), new NativeSegment("java", @"\ude00", "owner", "member", [])),
            WithSegments(new TextSegment(@"\u005c"), new ReferenceSegment(new("example", "uE000", []))),
            new([new("first", "example", [new TextSegment("\\")]), new("second", "example", [new TextSegment("uE000")])], []),
        ];
        _errors = [.. plans.Select(plan => Catch.Exception(() => JavaLanguagePack.Validate(plan)))];
    }

    [Fact] void should_validate_literal_runs_once_and_keep_files_separate() => _errors.ShouldEachConformTo(error => error == null);
}
