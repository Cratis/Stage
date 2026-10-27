// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Languages.Java.for_JavaLanguagePack;

public class when_a_field_contains_a_malformed_unicode_escape : given.a_symbol_plan
{
    Exception?[] _errors = [];

    void Because() => _errors = [.. WithEachField(@"\u12G4").Select(plan => Catch.Exception(() => JavaLanguagePack.Validate(plan)))];

    [Fact] void should_report_an_integrity_refusal_in_every_field() =>
        _errors.Cast<LanguagePackRefusal>().Select(error => error.Code).ShouldEachConformTo(code => code == JavaRefusalCodes.MalformedUnicodeEscape);
}
