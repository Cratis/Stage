// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Languages.Java.for_InlineBodyScanner;

public class when_scanning_a_literal_split_across_text_segments : given.a_native_body
{
    void Because() => _identifiers = InlineBodyScanner.Identifiers(new SymbolFile("file", "p", [new TextSegment("\"String "), new TextSegment("List\"; int com;")]));

    [Fact] void should_keep_literal_state_across_fragments() => _identifiers.Contains("String").ShouldBeFalse();
    [Fact] void should_skip_the_rest_of_the_literal() => _identifiers.Contains("List").ShouldBeFalse();
    [Fact] void should_resume_for_generated_identifiers() => _identifiers.Contains("com").ShouldBeTrue();
}
