// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Languages.Java.for_InlineBodyScanner.when_scanning;

public class and_comments_contain_type_names : given.a_native_body
{
    void Establish() => _body = "// String\r\n/* List */ Instant now;";
    void Because() => Scan();

    [Fact] void should_skip_line_comments() => _identifiers.Contains("String").ShouldBeFalse();
    [Fact] void should_skip_block_comments() => _identifiers.Contains("List").ShouldBeFalse();
    [Fact] void should_reserve_the_type() => _identifiers.Contains("Instant").ShouldBeTrue();
    [Fact] void should_reserve_the_local() => _identifiers.Contains("now").ShouldBeTrue();
}
