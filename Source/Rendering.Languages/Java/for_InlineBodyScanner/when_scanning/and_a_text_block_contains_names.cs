// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Languages.Java.for_InlineBodyScanner.when_scanning;

public class and_a_text_block_contains_names : given.a_native_body
{
    void Establish() => _body = "\"\"\"\nString /* List */ \\\"\"\" StillInside\n\"\"\"; Actual value;";
    void Because() => Scan();

    [Fact] void should_skip_text_block_contents() => _identifiers.Contains("String").ShouldBeFalse();
    [Fact] void should_not_treat_an_escaped_quote_as_the_closing_delimiter() => _identifiers.Contains("StillInside").ShouldBeFalse();
    [Fact] void should_resume_at_the_closing_delimiter() => _identifiers.Contains("Actual").ShouldBeTrue();
}
