// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Languages.Java.for_JavaLanguagePack;

public class when_an_eligible_trailing_backslash_precedes_a_reference : given.a_symbol_plan
{
    Exception?[] _errors = [];

    void Because() => _errors = [.. new[] { @"\", @"\\\", @"\u005c\", @"\u005c\\" }.SelectMany(text => new[]
        {
            WithSegments(new TextSegment(text), new ReferenceSegment(new("example", "uE000", []))),
            WithSegments(new NativeSegment("java", text, "owner", "member", []), new ReferenceSegment(new("example", "uE000", []))),
        }).Select(plan => Catch.Exception(() => JavaLanguagePack.Validate(plan)))];

    [Fact] void should_refuse_the_unknown_continuation_before_resolution() =>
        _errors.Cast<LanguagePackRefusal>().Select(error => error.Code).ShouldEachConformTo(code => code == JavaRefusalCodes.UnsafeLiteralBoundary);
}
