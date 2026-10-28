// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Languages.Java.for_JavaIdentifiers;

public class when_a_sanitized_name_is_too_long : Specification
{
    Exception? _error;

    void Because() => _error = Catch.Exception(() => JavaIdentifiers.Sanitize(new string('a', 256)));

    [Fact] void should_refuse_an_invalid_identifier() => _error.ShouldBeOfExactType<LanguagePackRefusal>();
    [Fact] void should_report_the_identifier_refusal() => ((LanguagePackRefusal)_error!).Code.ShouldEqual(JavaRefusalCodes.InvalidIdentifier);
}
