// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Languages.Java.for_JavaOutputIntegrity;

public class when_complete_output_contains_decoded_private_use : Specification
{
    Exception?[] _errors = [];

    void Because() => _errors = [.. new[] { @"\uE000", @"\uuuuF8ff", @"\udb80\udc00", @"\udbbf\udffd", @"\udbc0\udc00", @"\udbff\udffd", @"\\\uE000" }
        .Select(value => "class Example {} // " + value + "\n")
        .Select(text => Catch.Exception(() => JavaOutputIntegrity.Validate(text)))];

    [Fact] void should_refuse_bmp_and_both_supplementary_private_use_ranges_after_whole_file_translation() =>
        _errors.Cast<LanguagePackRefusal>().Select(error => error.Code).ShouldEachConformTo(code => code == JavaRefusalCodes.PrivateUseCodePoint);
}
