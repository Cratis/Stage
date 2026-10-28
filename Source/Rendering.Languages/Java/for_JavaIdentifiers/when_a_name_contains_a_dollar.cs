// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Languages.Java.for_JavaIdentifiers;

public class when_a_name_contains_a_dollar : Specification
{
    Exception? _error;

    void Because() => _error = Catch.Exception(() => JavaIdentifiers.Sanitize("Outer$Inner"));

    [Fact] void should_refuse_rather_than_rewrite_the_name() => _error.ShouldBeOfExactType<LanguagePackRefusal>();
    [Fact] void should_report_the_identifier_refusal() => ((LanguagePackRefusal)_error!).Code.ShouldEqual(JavaRefusalCodes.InvalidIdentifier);
}
