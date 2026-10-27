// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Languages.Java.for_JavaOutputIntegrity;

public class when_complete_output_contains_a_malformed_eligible_escape : Specification
{
    Exception?[] _errors = [];

    void Because() => _errors = [.. new[] { @"\u", @"\uuuu", @"\u123", @"\u12G4", @"\\\uZZZZ", @"\u005c\u12G4" }
        .Select(text => Catch.Exception(() => JavaOutputIntegrity.Validate(text)))];

    [Fact] void should_refuse_malformed_escapes_using_the_existing_integrity_code() =>
        _errors.Cast<LanguagePackRefusal>().Select(error => error.Code).ShouldEachConformTo(code => code == JavaRefusalCodes.MalformedUnicodeEscape);
}
