// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Languages.Java.for_InlineBodyScanner.when_scanning;

public class and_literals_contain_type_names : given.a_native_body
{
    void Establish() => _body = "\"String \\\" List\"; 'x'; '\\\''; Actual value;";
    void Because() => Scan();

    [Fact] void should_skip_strings_and_escaped_quotes() => _identifiers.Contains("String").ShouldBeFalse();
    [Fact] void should_stay_inside_an_escaped_quote() => _identifiers.Contains("List").ShouldBeFalse();
    [Fact] void should_skip_characters() => _identifiers.Contains("x").ShouldBeFalse();
    [Fact] void should_resume_after_the_literal() => _identifiers.Contains("Actual").ShouldBeTrue();
}
