// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Languages.Java.for_JavaLanguagePack;

public class when_a_private_use_escape_straddles_literal_segments : given.a_symbol_plan
{
    Exception?[] _errors = [];

    void Because()
    {
        SymbolPlan[] plans =
        [
            WithSegments(new TextSegment("\\"), new TextSegment("uE000")),
            WithSegments(new TextSegment("\\"), new NativeSegment("java", "uE000", "owner", "member", [])),
            WithSegments(new NativeSegment("java", "\\", "owner", "member", []), new TextSegment("uE000")),
            WithSegments(new NativeSegment("java", "\\", "owner", "member", []), new NativeSegment("java", "uE000", "owner", "member", [])),
            WithSegments(new TextSegment("\\"), new TextSegment(""), new TextSegment("uE000")),
        ];
        _errors = [.. plans.Select(plan => Catch.Exception(() => JavaLanguagePack.Validate(plan)))];
    }

    [Fact] void should_conservatively_refuse_the_raw_backslash_boundary_including_an_empty_following_text() =>
        _errors.Cast<LanguagePackRefusal>().Select(error => error.Code).ShouldEachConformTo(code => code == JavaRefusalCodes.UnsafeLiteralBoundary);
}
