// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Languages.Java.for_JavaLanguagePack;

public class when_a_decoded_trailing_backslash_precedes_imports : given.a_symbol_plan
{
    Exception?[] _errors = [];

    void Because()
    {
        SymbolPlan[] plans =
        [
            WithSegments(new TextSegment(@"\u005c"), new ImportsSegment(), new TextSegment(@"\\uE000")),
            WithSegments(new NativeSegment("java", @"\u005c", "owner", "member", []), new ImportsSegment(), new TextSegment(@"\\uE000")),
            WithSegments(new TextSegment(@"\u005c"), new TextSegment(""), new TextSegment(@"\\uE000")),
            WithSegments(new NativeSegment("java", @"\u005c", "owner", "member", []), new TextSegment("")),
        ];
        _errors = [.. plans.Select(plan => Catch.Exception(() => JavaLanguagePack.Validate(plan)))];
    }

    [Fact] void should_refuse_even_when_the_following_segment_might_emit_nothing() =>
        _errors.Cast<LanguagePackRefusal>().Select(error => error.Code).ShouldEachConformTo(code => code == JavaRefusalCodes.UnsafeLiteralBoundary);
}
