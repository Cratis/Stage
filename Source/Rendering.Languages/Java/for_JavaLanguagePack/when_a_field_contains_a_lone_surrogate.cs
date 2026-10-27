// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Languages.Java.for_JavaLanguagePack;

public class when_a_field_contains_a_lone_surrogate : given.a_symbol_plan
{
    Exception?[] _errors = [];

    void Because() => _errors = [.. new[] { "\ud800", "\udc00", "a\ud800b", "\ud800\ud800", "\udc00\ud800", @"\ud800", @"\udc00", @"\ud800x", @"\ud800\ud800" }
        .SelectMany(WithEachField).Select(plan => Catch.Exception(() => JavaLanguagePack.Validate(plan)))];

    [Fact] void should_refuse_raw_and_decoded_lone_surrogates_in_every_field() =>
        _errors.Cast<LanguagePackRefusal>().Select(error => error.Code).ShouldEachConformTo(code => code == JavaRefusalCodes.LoneSurrogate);
}
