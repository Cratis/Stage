// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Languages.Java.for_JavaLanguagePack;

public class when_an_eligible_trailing_backslash_precedes_other_unresolved_segments : given.a_symbol_plan
{
    Exception?[] _errors = [];

    void Because()
    {
        SymbolSegment[] unresolved =
        [
            new NameSegment(new("example", "uE000", [])),
            new DeclarationSegment(new("example", "uE000", []), "owner"),
            new ImportsSegment(),
            new GapSegment("existing-code", "construct", "intent", "member"),
        ];
        _errors = [.. unresolved.Select(segment => WithSegments(new TextSegment("\\"), segment))
            .Select(plan => Catch.Exception(() => JavaLanguagePack.Validate(plan)))];
    }

    [Fact] void should_refuse_the_unknown_continuation_for_every_non_literal_kind() =>
        _errors.Cast<LanguagePackRefusal>().Select(error => error.Code).ShouldEachConformTo(code => code == JavaRefusalCodes.UnsafeLiteralBoundary);
}
