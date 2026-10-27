// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Languages.Java.for_JavaLanguagePack;

public class when_a_segment_contains_a_private_use_code_point : given.a_symbol_plan
{
    Exception?[] _errors = [];

    void Because() => _errors = [.. new[] { 0xE000, 0xF8FF, 0xF0000, 0xFFFFD, 0x100000, 0x10FFFD }
        .Select(char.ConvertFromUtf32).SelectMany(WithEachField).Select(plan => Catch.Exception(() => JavaLanguagePack.Validate(plan)))];

    [Fact] void should_refuse_bmp_and_both_supplementary_private_use_ranges_in_every_field() =>
        _errors.Cast<LanguagePackRefusal>().Select(error => error.Code).ShouldEachConformTo(code => code == JavaRefusalCodes.PrivateUseCodePoint);
}
