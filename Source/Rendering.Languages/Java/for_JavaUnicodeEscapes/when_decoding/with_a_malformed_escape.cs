// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Languages.Java.for_JavaUnicodeEscapes.when_decoding;

public class with_a_malformed_escape : Specification
{
    Exception?[] _errors = [];

    void Because() => _errors = [.. new[] { @"\u", @"\uuu", @"\u123", @"\u12G4", @"\u+123", @"\u 123", @"\\\uu0z00" }.Select(text => Catch.Exception(() => JavaUnicodeEscapes.Decode(text)))];

    [Fact] void should_refuse_each_malformed_eligible_escape() => _errors.Cast<LanguagePackRefusal>().Select(error => error.Code).ShouldEachConformTo(code => code == JavaRefusalCodes.MalformedUnicodeEscape);
}
