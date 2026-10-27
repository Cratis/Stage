// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Languages.Java.for_JavaOutputIntegrity;

public class when_complete_output_contains_raw_private_use : Specification
{
    Exception?[] _errors = [];

    void Because() => _errors = [.. new[] { 0xE000, 0xF8FF, 0xF0000, 0xFFFFD, 0x100000, 0x10FFFD }
        .Select(value => "class Example {} // " + char.ConvertFromUtf32(value) + "\n")
        .Select(text => Catch.Exception(() => JavaOutputIntegrity.Validate(text)))];

    [Fact] void should_refuse_bmp_and_both_supplementary_private_use_ranges() =>
        _errors.Cast<LanguagePackRefusal>().Select(error => error.Code).ShouldEachConformTo(code => code == JavaRefusalCodes.PrivateUseCodePoint);
}
