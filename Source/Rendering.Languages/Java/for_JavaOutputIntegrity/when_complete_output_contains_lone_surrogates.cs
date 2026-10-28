// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Languages.Java.for_JavaOutputIntegrity;

public class when_complete_output_contains_lone_surrogates : Specification
{
    Exception?[] _errors = [];

    void Because() => _errors = [.. new[] { "\ud800", "\udc00", "\ud800x", "\ud800\ud800", @"\ud800", @"\udc00", @"\udc00\ud800", @"\ud800x" }
        .Select(value => "class Example {} // " + value + "\n")
        .Select(text => Catch.Exception(() => JavaOutputIntegrity.Validate(text)))];

    [Fact] void should_refuse_raw_and_decoded_unpaired_surrogates() =>
        _errors.Cast<LanguagePackRefusal>().Select(error => error.Code).ShouldEachConformTo(code => code == JavaRefusalCodes.LoneSurrogate);
}
