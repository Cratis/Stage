// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Languages.Java.for_JavaLanguagePack;

public class when_a_lone_surrogate_escape_straddles_literal_segments : given.a_symbol_plan
{
    Exception?[] _errors = [];

    void Because()
    {
        SymbolPlan[] plans =
        [
            WithSegments(new TextSegment("\\"), new TextSegment("ud800")),
            WithSegments(new TextSegment("\\"), new TextSegment("udc00")),
            WithSegments(new TextSegment("\\"), new NativeSegment("java", "ud800", "owner", "member", [])),
            WithSegments(new NativeSegment("java", "\\", "owner", "member", []), new TextSegment("ud800")),
        ];
        _errors = [.. plans.Select(plan => Catch.Exception(() => JavaLanguagePack.Validate(plan)))];
    }

    [Fact] void should_conservatively_refuse_the_raw_backslash_boundary() =>
        _errors.Cast<LanguagePackRefusal>().Select(error => error.Code).ShouldEachConformTo(code => code == JavaRefusalCodes.UnsafeLiteralBoundary);
}
