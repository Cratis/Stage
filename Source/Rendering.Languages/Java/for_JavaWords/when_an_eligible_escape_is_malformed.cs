// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Languages.Java.for_JavaWords;

public class when_an_eligible_escape_is_malformed : Specification
{
    Exception _error = null!;

    void Because() => _error = Catch.Exception(() => JavaWords.Scan(@"text \u00xz"));

    [Fact] void should_refuse_with_the_unicode_escape_code() => ((LanguagePackRefusal)_error).Code.ShouldEqual(JavaRefusalCodes.MalformedUnicodeEscape);
}
