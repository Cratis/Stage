// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Languages.Java.for_JavaLanguagePack;

public class and_text_spells_a_private_use_char_as_an_ascii_escape : given.a_symbol_plan
{
    Exception?[] _errors = [];

    void Because() => _errors = [.. new[] { @"\uE000", @"\uuuuF8ff", @"\udb80\udc00", @"\udbbf\udffd", @"\udbc0\udc00", @"\udbff\udffd", @"\\\uE000", @"\u005c\uE000" }
        .SelectMany(WithEachField).Select(plan => Catch.Exception(() => JavaLanguagePack.Validate(plan)))];

    [Fact] void should_refuse_private_use_values_after_eligible_translation_in_every_field() =>
        _errors.Cast<LanguagePackRefusal>().Select(error => error.Code).ShouldEachConformTo(code => code == JavaRefusalCodes.PrivateUseCodePoint);
}
