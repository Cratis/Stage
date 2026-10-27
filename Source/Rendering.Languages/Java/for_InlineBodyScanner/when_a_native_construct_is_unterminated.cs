// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Languages.Java.for_InlineBodyScanner;

public class when_a_native_construct_is_unterminated : given.a_native_body
{
    LanguagePackRefusal[] _errors = [];

    void Because() => _errors = [.. new[] { "/* String", "\"\"\"\nString", "\"String", "'x", "\"String\nActual" }
        .Select(body => (LanguagePackRefusal)Catch.Exception(() => InlineBodyScanner.Identifiers(body)))];

    [Fact] void should_refuse_instead_of_hiding_identifiers() => _errors.ShouldEachConformTo(error => error.Code == JavaRefusalCodes.UnterminatedNativeConstruct);
}
