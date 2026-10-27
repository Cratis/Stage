// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Languages.Java.for_InlineBodyScanner.when_scanning;

public class and_a_text_block_ends_after_an_even_backslash_run : given.a_native_body
{
    void Establish() => _body = "\"\"\"\nString \\\\\"\"\"; Actual value;";
    void Because() => Scan();

    [Fact] void should_skip_the_contents() => _identifiers.Contains("String").ShouldBeFalse();
    [Fact] void should_resume_after_the_even_run() => _identifiers.Contains("Actual").ShouldBeTrue();
}
